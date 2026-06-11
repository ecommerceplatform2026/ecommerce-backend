using System.Collections.Concurrent;
using System.Net.Http.Json;
using Application.Interfaces.Events;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using StackExchange.Redis;

namespace Ecommerce.PerformanceTests;

public sealed class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var value) && value is T typedValue)
            return Task.FromResult<T?>(typedValue);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (value != null) _cache[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var key in _cache.Keys.ToList()) { if (key.StartsWith(prefix)) _cache.TryRemove(key, out _); }
        return Task.CompletedTask;
    }

    public async Task<T?> GetOrAddAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var cached) && cached is T typedValue)
            return typedValue;
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(key, out cached) && cached is T inside)
                return inside;
            var value = await factory();
            if (value != null) _cache[key] = value;
            return value;
        }
        finally { semaphore.Release(); }
    }
}

public sealed class PerformanceTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = $"PerfTestDb_{Guid.NewGuid():N}";

    public string AdminToken { get; private set; } = string.Empty;
    public string UserToken { get; private set; } = string.Empty;
    public Guid AdminUserId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid VariantId { get; private set; }
    public Guid AddressId { get; private set; }
    public Guid PendingOrderId { get; private set; }
    public int PendingOrderCode { get; private set; }
    public Guid CompletedOrderId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<EcommerceContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);
            services.AddDbContext<EcommerceContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor != null) services.Remove(redisDescriptor);
            services.AddSingleton(new Mock<IConnectionMultiplexer>().Object);

            var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheDescriptor != null) services.Remove(cacheDescriptor);
            services.AddScoped<ICacheService, InMemoryCacheService>();

            var imageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProductImageStorage));
            if (imageDescriptor != null) services.Remove(imageDescriptor);
            services.AddScoped(_ => new Mock<IProductImageStorage>().Object);

            var notificationDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(INotificationService));
            if (notificationDescriptor != null) services.Remove(notificationDescriptor);
            services.AddScoped(_ => new Mock<INotificationService>().Object);

            var vnPayDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IVnPayService));
            if (vnPayDescriptor != null) services.Remove(vnPayDescriptor);
            var mockVnPay = new Mock<IVnPayService>();
            mockVnPay.Setup(x => x.CreatePaymentUrl(It.IsAny<int>(), It.IsAny<long>()))
                .Returns("https://mock-payment.test/pay");
            services.AddScoped(_ => mockVnPay.Object);

            var momoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMomoService));
            if (momoDescriptor != null) services.Remove(momoDescriptor);
            var mockMomo = new Mock<IMomoService>();
            mockMomo.Setup(x => x.CreatePaymentUrlAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://mock-payment.test/pay");
            services.AddScoped(_ => mockMomo.Object);

            var zaloDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IZaloPayService));
            if (zaloDescriptor != null) services.Remove(zaloDescriptor);
            var mockZalo = new Mock<IZaloPayService>();
            mockZalo.Setup(x => x.CreatePaymentUrlAsync(It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://mock-payment.test/pay");
            services.AddScoped(_ => mockZalo.Object);

            var constraintDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUniqueConstraintChecker));
            if (constraintDescriptor != null) services.Remove(constraintDescriptor);
            services.AddScoped(_ => new Mock<IUniqueConstraintChecker>().Object);

            var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var hs in hostedServices)
            {
                if (hs.ImplementationType?.Name is "PaymentTimeoutBackgroundService"
                    or "PointsExpiryBackgroundService"
                    or "DeliveredOrdersCompletionBackgroundService"
                    or "OutboxBackgroundProcessor")
                {
                    services.Remove(hs);
                }
            }
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EcommerceContext>();

        var adminUser = User.Create("Admin", "admin@test.com", BCrypt.Net.BCrypt.HashPassword("Admin123!"));
        var regularUser = User.Create("User", "user@test.com", BCrypt.Net.BCrypt.HashPassword("User123!"));
        context.Users.AddRange(adminUser, regularUser);
        await context.SaveChangesAsync(CancellationToken.None);

        AdminUserId = adminUser.Id;
        UserId = regularUser.Id;

        var category = Category.Create("Test Category");
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);
        CategoryId = category.Id;

        var product = Product.Create(category.Id, "Performance Test Product", "Product description for perf testing", "Cotton", new Money(150000), ProductStatus.Active);
        context.Products.Add(product);
        await context.SaveChangesAsync(CancellationToken.None);
        ProductId = product.Id;

        var variant = ProductVariant.Create(product.Id, new Sku("PERF-V1"), "Red", "M", 1000, new Money(150000));
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync(CancellationToken.None);
        VariantId = variant.Id;

        var address = UserAddress.Create(regularUser.Id, "Test User", "0900000000", "123 Test Street", "Ward 1", "District 1", "HCMC", true);
        context.UserAddresses.Add(address);
        await context.SaveChangesAsync(CancellationToken.None);
        AddressId = address.Id;

        var pendingOrder = Domain.Entities.Order.Create(regularUser.Id, new Random().Next(100000, 999999), PaymentMethod.COD);
        pendingOrder.AddItem(variant.Id, 1, new Money(150000), "Performance Test Product - Red/M");
        context.Orders.Add(pendingOrder);
        await context.SaveChangesAsync(CancellationToken.None);
        PendingOrderId = pendingOrder.Id;
        PendingOrderCode = pendingOrder.OrderCode;

        var completedOrder = Domain.Entities.Order.Create(regularUser.Id, new Random().Next(100000, 999999), PaymentMethod.COD);
        completedOrder.AddItem(variant.Id, 1, new Money(150000), "Performance Test Product - Red/M");
        completedOrder.MarkAsConfirmed();
        completedOrder.MarkAsProcessing();
        completedOrder.MarkAsShipping();
        completedOrder.MarkAsDelivered();
        completedOrder.MarkAsCompleted();
        context.Orders.Add(completedOrder);
        await context.SaveChangesAsync(CancellationToken.None);
        CompletedOrderId = completedOrder.Id;

        var loyaltyAccount = LoyaltyAccount.Create(regularUser.Id);
        loyaltyAccount.AddAvailablePoints(5000);
        context.LoyaltyAccounts.Add(loyaltyAccount);
        await context.SaveChangesAsync(CancellationToken.None);

        var client = CreateClient();

        var adminLogin = await client.PostAsJsonAsync("/api/auth/login", new { Email = "admin@test.com", Password = "Admin123!" });
        var adminResult = await adminLogin.Content.ReadFromJsonAsync<Presentation.Common.Responses.ApiResponse<Application.DTOs.Auth.AuthResponse>>();
        AdminToken = adminResult!.Data!.Token;

        var userLogin = await client.PostAsJsonAsync("/api/auth/login", new { Email = "user@test.com", Password = "User123!" });
        var userResult = await userLogin.Content.ReadFromJsonAsync<Presentation.Common.Responses.ApiResponse<Application.DTOs.Auth.AuthResponse>>();
        UserToken = userResult!.Data!.Token;
    }

    public new Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public HttpClient CreateAuthenticatedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
