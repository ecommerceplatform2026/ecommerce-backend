# Fashion E-Commerce Backend API

A high-performance, robust, and clean women's fashion e-commerce backend platform built using **ASP.NET Core Web API**, **.NET 8**, **Entity Framework Core (EF Core)**, and **PostgreSQL**.

The platform is designed following **Clean Architecture** principles and includes enterprise-grade features such as optimistic concurrency protection, partitioned rate limiting, automatic transient connection retries, unified caching with Redis, and physical HTML template-based notifications.

---

## 📖 Table of Contents
1. [Tech Stack](#tech-stack)
2. [Setup & Installation Guide](#setup--installation-guide)
3. [Architecture Overview](#architecture-overview)
4. [Database Design](#database-design)
5. [Caching Strategy](#caching-strategy)
6. [API Catalog Reference](#api-catalog-reference)
7. [Concurrency & Resilience Engineering](#concurrency--resilience-engineering)

---

## 🛠 Tech Stack

- **Framework**: .NET 8 (ASP.NET Core Web API)
- **Database**: PostgreSQL (via Npgsql Entity Framework Core Provider)
- **Caching**: Redis (via StackExchange.Redis & StackExchangeRedisCache)
- **Object Relational Mapper (ORM)**: EF Core 8
- **Resilience & Fault Tolerance**: Microsoft Extensions Http Resilience (Polly-based HTTP retry policies)
- **Rate Limiting**: Built-in partitioned fixed-window rate limiter
- **Security**: JWT Authentication & Role-Based Authorization, BCrypt Password Hashing
- **Notifications**: SMTP Mail Service (with logger support & physical HTML templates)
- **Image Storage**: Cloudinary Cloud Image Hosting API
- **API Documentation**: Swagger / Swashbuckle OpenAPI v3

---

## ⚙ Setup & Installation Guide

### Prerequisites
- **.NET 8 SDK** installed.
- **PostgreSQL** server running locally or accessible remotely.
- **Redis** server running locally (`localhost:6379`) or remotely.

### 1. Clone the Repository
```bash
git clone https://github.com/ecommerceplatform2026/ecommerce-backend.git
cd ecommerce-backend
```

### 2. Configure Local Application Secrets
Update or create `appsettings.Development.json` under `Ecommerce/Presentation/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ecommerce_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "Secret": "A_VERY_STRONG_SECRET_KEY_OF_AT_LEAST_32_CHARACTERS",
    "Issuer": "EcommerceBackend",
    "Audience": "EcommerceFrontend",
    "ExpirationHours": 24
  },
  "CloudinarySettings": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret",
    "Folder": "ecommerce_products"
  },
  "VnPay": {
    "TmnCode": "your-vnpay-terminal-code",
    "HashSecret": "your-vnpay-secure-hash-secret",
    "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ReturnUrl": "https://localhost:<httpsPort>/api/payments/vnpay-return"
  },
  "MailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "From": "your-sender-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

### 3. Apply EF Core Migrations
Ensure the database schema is fully initialized:
```bash
dotnet ef database update --project Ecommerce/Infrastructure --startup-project Ecommerce/Presentation
```

### 4. Run the Application
```bash
dotnet run --project Ecommerce/Presentation
```

### 5. Access Interactive Documentation
Open the interactive Swagger OpenAPI UI at:
```text
https://localhost:<port>/swagger
```

---

## 🏗 Architecture Overview

The system is structured according to **Clean Architecture** patterns, separating concerns strictly into isolated layers. The dependencies only flow inwards.

```text
       ┌────────────────────────────────────────────────────────┐
       │                       Presentation                     │
       │  (Controllers, Middlewares, Program.cs, Extensions)    │
       └───────────────────────────┬────────────────────────────┘
                                   │
                                   ▼
       ┌────────────────────────────────────────────────────────┐
       │                        Application                     │
       │ (Interfaces, DTOs, Services, Mappings, Caching keys)   │
       └───────────────────────────┬────────────────────────────┘
                                   │
                                   ▼
       ┌────────────────────────────────────────────────────────┐
       │                        Domain                          │
       │  (Entities, Enums, TimeHelpers, Core Base Entity)      │
       └───────────────────────────▲────────────────────────────┘
                                   │
                                   │
       ┌───────────────────────────┴────────────────────────────┐
       │                       Infrastructure                   │
       │ (EF Context, Configurations, Services implementations) │
       └────────────────────────────────────────────────────────┘
```

- **Domain Layer**: Holds aggregate roots, entities, domain enums, and common base properties. It has zero external dependencies.
- **Application Layer**: Contains business logic interfaces, DTO request/response schemas, mapping extension methods, and application service implementations (`ProductService`, `CheckoutService`, `CartService`).
- **Infrastructure Layer**: Implements database persistence (`EcommerceContext`), configurations (fluent mapping files), migrations, and external service clients (Redis, Cloudinary, SMTP dispatcher).
- **Presentation Layer**: Exposes Web API controllers, custom exception filters, partitioned rate limiters, HTTP request pipeline middlewares, and setup hooks.

---

## 🗄 Database Design

The relational database schema is configured for clean referential integrity, cascading protections, index optimization, soft-delete filtering, and concurrency checking.

### ER Diagram Overview & Relationships

```text
 ┌──────────────┐         ┌──────────────┐         ┌────────────────┐
 │   Category   │◄────────│   Product    │◄────────│  ProductImage  │
 └──────────────┘         └──────┬───────┘         └────────────────┘
                                 │
                                 ▼
 ┌──────────────┐         ┌──────────────┐         ┌────────────────┐
 │     User     │◄────────│ProductVariant│◄────────│    CartItem    │
 └──────┬───────┘         └──────┬───────┘         └────────────────┘
        │                        │
        ▼                        ▼
 ┌──────────────┐         ┌──────────────┐         ┌────────────────┐
 │    Order     │◄────────│  OrderItem   │◄────────│     Review     │
 └──────┬───────┘         └──────────────┘         └────────────────┘
        │
        ▼
 ┌──────────────┐
 │   Payment    │
 └──────────────┘
```

- **User**: Stores profiles, credential hashes, and addresses.
- **Category & Product**: A category holds many products. Deleting a category is rejected if it contains active products.
- **ProductVariant**: Holds inventory stocks, colors, sizes, and pricing. Contains `IsConcurrencyToken()` on the `Stock` property.
- **CartItem**: Junction table managing the shopping cart items per user.
- **Order & OrderItem**: Tracks transaction history. `OrderItem` stores a full `ProductSnapshot` JSON string recording item details at the precise moment of purchase.
- **Payment**: Tracks digital gateway payments (VNPay/COD). Linked 1:1 with an `Order`.
- **Review**: Enforces verification that a user has actually purchased the product variant (and that order status is neither `Pending` nor `Cancelled`) before allowing review creation.

### Database Indexing Strategy
- **Unique Non-Deleted SKU**: Unique index on `ProductVariant.SKU` filtered via `WHERE "IsDeleted" = false` to allow reuse of SKUs on deleted entries.
- **Email Uniqueness**: Unique index on `User.Email` to prevent duplicate registration.
- **Query Filter**: All entities inheriting from `BaseEntity` automatically apply the EF Core global query filter: `modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted)`.

---

## 🚀 Caching Strategy

The caching system is built to minimize database stress while maintaining data accuracy.

### Cache Keys & Lifespans
- **Categories List**: `categories:all` (Time-to-Live: 1 Hour)
- **Products Catalog List**: `products:all` (Time-to-Live: 1 Hour)
- **Product Detail**: `products:detail:{id}` (Time-to-Live: 1 Hour)

### Cache Invalidation Hooks
To prevent users from viewing stale inventory or pricing, specific caching entries are invalidated dynamically:
- **Product / Variant Modifications**: Modifying a product or adding/deleting a variant clears the product list (`products:all`) and specific product detail cache (`products:detail:{productId}`).
- **Stock Updates**: Invalidation is triggered on stock changes at:
  - Successful **Checkout** (`CheckoutService.cs`).
  - Failed **Payment Callbacks** when restoring inventory (`PaymentService.cs`).
  - Order **Payment Timeout Background Service** cancels unpaid orders and restores inventory (`PaymentTimeoutBackgroundService.cs`).
- **Category Modifications**: Modifying a category clears all category listings (`categories:all`) and all product detail listings via prefix wildcard eviction (`products:*`).

### Transient Resiliency Policy
All cache invalidation calls to Redis are wrapped inside resilient catch blocks. In case of a Redis connection outage, the core business flows (checkout, callback, or deletion) **continue to work seamlessly** without failing.

---

## 📞 API Catalog Reference

Here is a summary of the most critical endpoints exposed by the platform:

### 🔐 Authentication (`api/auth`)
- `POST /api/auth/register` - Create customer account (Rate Limit: 5 requests/min per IP).
- `POST /api/auth/login` - Authenticate and retrieve JWT token (Rate Limit: 5 requests/min per IP).

### 🛒 Shopping Cart (`api/cart`)
- `GET /api/cart` - View current customer cart (Eagerly loads images and variant details).
- `POST /api/cart/items` - Add product variant to cart (Enforces stock limit checks).
- `PUT /api/cart/items/{variantId}` - Update cart item quantity (Rejects quantity exceeding stock).
- `DELETE /api/cart/items/{variantId}` - Remove item from cart.
- `POST /api/cart/merge` - Bulk-merges local guest cart items with DB cart on login.

### 💳 Checkout & Payments (`api/checkout` & `api/payments`)
- `POST /api/checkout` - Deducts inventory, creates Order/Payment, clears cart (Rate Limit: 10 requests/min per User).
- `GET /api/payments/vnpay-return` - Receives callback from VNPay to capture checkout payment success/failure.

### 📦 Order History (`api/orders`)
- `GET /api/orders` - View paginated, status-filtered, chronological purchase history.

### ⭐ Product Reviews (`api/reviews`)
- `POST /api/reviews` - Submit review/rating (Restricted to verified purchasers only).

### 📊 Admin Dashboard (`api/admin/dashboard`)
- `GET /api/admin/dashboard/summary` - Fetch order counts, low-stock warnings, and top-selling products (Admin role only, supports date-range filtering).

---

## 🛡 Concurrency & Resilience Engineering

### 1. Concurrency-Safe Checkout (Oversell Prevention)
When multiple users checkout the same item simultaneously:
1. **Concurrency Check**: EF Core executes stock deductions using optimistic locking:
   ```sql
   UPDATE "ProductVariants" SET "Stock" = @newStock WHERE "Id" = @id AND "Stock" = @oldStock;
   ```
2. **Detection & Rollback**: If another thread modified the stock in the meantime, EF Core throws a `DbUpdateConcurrencyException`. The active database transaction rolls back.
3. **Change Tracker Clean & Reload**: The system invokes `_unitOfWork.ClearTracker()` to purge stale tracked states and reloads fresh database records.
4. **Retry Loop**: The process automatically retries up to **3 times** with linear backoff delays (for example, `100 ms × attempt`). If the item goes completely out of stock during retries, it throws a safe validation error to the customer.

### 2. Built-in Partitioned Rate Limiting
Protects the platform's sensitive public routes:
- **Authentication Routes** (`auth-limiter`): Allows up to **5 requests per 60 seconds**, partitioned **by client `RemoteIpAddress`, with fallback to `Host` when the remote IP is unavailable**.
- **Checkout Route** (`checkout-limiter`): Allows up to **10 requests per 60 seconds**, partitioned **by authenticated `User ID`, with fallback to `RemoteIpAddress`, and finally to `"anonymous"` when neither is available**.
- **Proxy deployment note**: When running behind a reverse proxy or load balancer, forwarded headers must be configured correctly so `RemoteIpAddress` reflects the real client IP instead of the proxy IP.

### 3. Fault-Tolerant Middlewares
- **PostgreSQL Connection Retry**: The Npgsql driver is configured with `EnableRetryOnFailure(3)` to automatically recover from transient network drops between the web server and database server.
- **Polly HTTP Resilience**: The external image service (`CloudinaryProductImageStorage`) is decorated with standard HTTP resilience handlers to perform automatic retries and circuit breakers on transient failures.
