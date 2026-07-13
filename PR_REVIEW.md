# US 6.19 PR Review — Decouple Background Tasks from Checkout Request (ABC-360)

This PR decouples third-party side effects (email sending, GHN delivery creation) from the checkout HTTP request lifecycle using the outbox pattern. Side effects are queued as integration events during the checkout transaction and processed asynchronously by a background worker with retry resilience.

## What

- `feat(events): split event hierarchy into domain (sync) and integration (async)` — Refactored `IDomainEvent` into `IEvent` (base), `IDomainEvent : IEvent` (sync dispatch), and `IIntegrationEvent : IEvent` (async outbox queue). `OrderConfirmedDomainEvent` now implements both interfaces — sync handlers (loyalty, cache) run inline, integration handlers (email, GHN delivery) go through the outbox.

- `feat(outbox): add table for background task queue` — New `OutboxMessage` entity with `EventType`, `JsonContent`, `CreatedAt`, `ProcessedAt`, `RetryCount`, `LastError`. EF configuration with composite index on `(ProcessedAt, CreatedAt)` for efficient querying of unprocessed messages. Migration `20260710071140_AddOutboxMessages`.

- `feat(outbox): add background worker to process integration events` — `OutboxBackgroundService` polls unprocessed messages every 1s (batch of 10), deserializes them via Newtonsoft.Json, and dispatches to `IIntegrationEventHandler<T>` implementations with a Polly retry pipeline (exponential backoff, max 2 per-handler attempts, 5 total message retries before dead-letter). `Newtonsoft.Json` package added to Infrastructure for reliable deserialization of domain entities with parameterized constructors and private setters.

- `fix(serialization): add [JsonProperty] to BaseEntity.Id` — Newtonsoft.Json ignores `protected set` by default. Without `[JsonProperty]`, deserialized entities get a random `Guid.NewGuid()` instead of the real `Id` from JSON, causing `ShippingService` to query the wrong GUID → "Order not found." Fixed by adding `[JsonProperty]` on `BaseEntity.Id` and `Newtonsoft.Json` package to the Domain project.

- `fix(delivery): let outbox retry handle GHN failures` — Removed `try/catch` from `CreateDeliveryOnOrderConfirmedHandler` that swallowed all exceptions. Outbox Polly pipeline now properly retries transient failures (GHN API timeouts) and dead-letters after max attempts.

- `chore(outbox): add handler resolution logging` — Added structured logging in `ProcessMessageAsync` to log handler count, handler names, and completion per message for operational debugging.

- `feat: handler registration refactored` — `DomainEventPublisher` registered as `IDomainEventPublisher` (sync dispatch, filters by `IDomainEvent`). `OutboxPublisher` registered as `IIntegrationEventPublisher` (async queue, filters by `IIntegrationEvent`). `SendOrderConfirmationEmailHandler` and `CreateDeliveryOnOrderConfirmedHandler` changed from `IDomainEventHandler<T>` to `IIntegrationEventHandler<T>`. `UnitOfWork` calls both publishers per event — each internally filters by interface type.

## Why

- **Checkout latency:** Email and GHN delivery creation previously ran synchronously in the checkout request. If email provider or GHN API is slow or down, the customer sees a timeout or error even though the order was saved successfully. The outbox pattern returns the HTTP response immediately after saving the order and queues side effects for background processing.

- **Reliability:** Background processing includes automatic retries with exponential backoff for transient failures (email provider timeout, GHN API rate limit). After exhausting retries (5 attempts), the message is preserved in the database for manual investigation without blocking other messages.

- **No data loss:** Outbox messages are persisted in the same database transaction as the business data (order, payment). If the application crashes before the background worker processes them, unprocessed messages are picked up automatically on restart (AC4).

- **Newtonsoft.Json vs STJ:** System.Text.Json cannot deserialize domain entities with parameterized constructors or `private set` properties. Newtonsoft.Json with `TypeNameHandling.Auto` handles polymorphic event deserialization required for the outbox.

- **GHN handler exception swallowing:** The old `try/catch` in `CreateDeliveryOnOrderConfirmedHandler` swallowed ALL exceptions, including transient GHN API failures. This meant the outbox always marked the message as processed (`ProcessedAt` set) even when the delivery was never created. Removed the `try/catch` so the outbox Polly pipeline handles retries properly.

## How

- **Event hierarchy:** `IEvent` (base interface with `OccurredOn`) → `IDomainEvent : IEvent` (sync, dispatched inline by `DomainEventPublisher`) + `IIntegrationEvent : IEvent` (async, queued to outbox by `OutboxPublisher`). `OrderConfirmedDomainEvent` implements both, so it gets dispatched via both paths.

- **Dual dispatch in UnitOfWork:** `SaveChangesAsync` collects domain events from tracked entities, calls `IDomainEventPublisher.PublishAsync` (runs sync handlers like loyalty point creation and cache invalidation immediately) then `IIntegrationEventPublisher.PublishAsync` (serializes the event to JSON with `TypeNameHandling.Auto` and `ReferenceLoopHandling.Ignore` and adds an `OutboxMessage` to the DbContext). Both happen before `context.SaveChangesAsync`, so the outbox message is persisted atomically with the business data.

- **OutboxBackgroundService:** Polls `OutboxMessages` where `ProcessedAt IS NULL AND RetryCount < 5` ordered by `CreatedAt` in batches of 10. For each message: deserializes `EventType` via `Type.GetType` + `JsonConvert.DeserializeObject`, resolves `IIntegrationEventHandler<T>` handlers via DI, executes each via a Polly `ResiliencePipeline` with `MaxRetryAttempts = 2` and exponential backoff. On handler success → sets `ProcessedAt`. On exception → increments `RetryCount`, logs warning, stores `LastError`. After 5 failed attempts, the message stays in the table with `RetryCount = 5` for manual investigation.

- **Id deserialization fix:** `BaseEntity.Id` has `protected set`. Newtonsoft.Json by default skips properties with non-public setters during deserialization. `[JsonProperty]` explicitly marks `Id` for deserialization, so the deserialized `Order` object retains the correct GUID from JSON instead of the random one generated by the `Guid.NewGuid()` initializer in the parameterless constructor.

- **DI wiring:** Both handler types (`IDomainEventHandler<>` and `IIntegrationEventHandler<>`) are scanned from the Application assembly and registered as `AddScoped`. `OutboxBackgroundService` uses `IServiceScopeFactory` to create a scope per batch, ensuring fresh `EcommerceContext` instances for each poll cycle.

## How to test

1. Place a COD order → verify `OutboxMessages` table has a row with `EventType = "Domain.Events.OrderConfirmedDomainEvent, Domain"` and `ProcessedAt = NULL`.
2. Wait ~1s → verify `ProcessedAt` is set, email is sent, GHN delivery is created.
3. Deliberately fail GHN API (set invalid API key) → verify `RetryCount` increments up to 5, then message dead-letters with `LastError` populated.
4. Restart the application while unprocessed messages exist → verify they're picked up and processed after restart.
5. Run `dotnet test` — verify all existing tests pass with no regressions.
