using Application.Interfaces.Events;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public sealed class OutboxBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxBackgroundService> _logger;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
        private const int MaxRetries = 5;
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
        };
        private static readonly ResiliencePipeline DefaultPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            })
            .Build();

        public OutboxBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxBackgroundService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox background service started.");

            using var timer = new PeriodicTimer(PollInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing outbox batch.");
                }
            }

            _logger.LogInformation("Outbox background service stopped.");
        }

        private async Task ProcessBatchAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EcommerceContext>();

            var messages = await context.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
                .OrderBy(m => m.CreatedAt)
                .Take(10)
                .ToListAsync(ct);

            foreach (var message in messages)
            {
                try
                {
                    await ProcessMessageAsync(message, scope, ct);
                    message.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.LastError = ex.ToString();
                    _logger.LogWarning(ex,
                        "Failed to process message {MessageId} (attempt {RetryCount}/{MaxRetries})",
                        message.Id, message.RetryCount, MaxRetries);
                }

                await context.SaveChangesAsync(ct);
            }
        }

        private async Task ProcessMessageAsync(OutboxMessage message, IServiceScope scope, CancellationToken ct)
        {
            var eventType = Type.GetType(message.EventType);
            if (eventType == null)
            {
                throw new InvalidOperationException($"Cannot load event type: {message.EventType}");
            }

            var @event = (IEvent)JsonConvert.DeserializeObject(message.JsonContent, eventType, JsonSettings)!;

            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
            var handlers = scope.ServiceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod == null) continue;

                await DefaultPipeline.ExecuteAsync(async cancel =>
                {
                    var task = (Task)handleMethod.Invoke(handler, new object[] { @event, cancel })!;
                    await task;
                }, ct);
            }
        }
    }
}
