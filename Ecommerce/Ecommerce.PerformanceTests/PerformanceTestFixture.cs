using System.Net.Http.Json;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;

namespace Ecommerce.PerformanceTests;

public sealed class PerformanceTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PerformanceTestContainers? _containers;

    public HttpClient Client { get; private set; } = null!;
    public string AdminToken { get; private set; } = string.Empty;
    public string UserToken { get; private set; } = string.Empty;

    public Guid ProductId { get; private set; }
    public Guid VariantId { get; private set; }
    public Guid AddressId { get; private set; }
    public Guid PendingOrderId { get; private set; }
    public int PendingOrderCode { get; private set; }
    public Guid CompletedOrderId { get; private set; }

    async Task IAsyncLifetime.InitializeAsync()
    {
        _containers = new PerformanceTestContainers();
        await _containers.InitializeAsync();

        Client = CreateClient();

        var seeder = new SeedDataGenerator(Services);
        var seedResult = await seeder.SeedAsync();

        ProductId = seedResult.ProductId;
        VariantId = seedResult.VariantId;
        AddressId = seedResult.AddressId;
        PendingOrderId = seedResult.PendingOrderId;
        PendingOrderCode = seedResult.PendingOrderCode;
        CompletedOrderId = seedResult.CompletedOrderId;

        AdminToken = await LoginAsync("admin@test.com", "Admin123!");
        UserToken = await LoginAsync("user@test.com", "User123!");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_containers != null)
        {
            await _containers.DisposeAsync();
            _containers = null;
        }
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            ReplaceWithPostgres(services);
            ReplaceWithRedis(services);
            MockExternalServices(services);
            RemoveBackgroundServices(services);
        });
    }

    private void ReplaceWithPostgres(IServiceCollection services)
    {
        var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<EcommerceContext>));
        if (dbDescriptor != null) services.Remove(dbDescriptor);

        services.AddDbContext<EcommerceContext>(options =>
            options.UseNpgsql(
                _containers!.PostgresConnectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));
    }

    private void ReplaceWithRedis(IServiceCollection services)
    {
        var muxDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
        if (muxDescriptor != null) services.Remove(muxDescriptor);

        var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
        if (cacheDescriptor != null) services.Remove(cacheDescriptor);

        var connString = _containers!.RedisConnectionString;
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(connString));
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = connString);
    }

    private static void MockExternalServices(IServiceCollection services)
    {
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

        var imageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProductImageStorage));
        if (imageDescriptor != null) services.Remove(imageDescriptor);
        services.AddScoped(_ => new Mock<IProductImageStorage>().Object);

        var notificationDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(INotificationService));
        if (notificationDescriptor != null) services.Remove(notificationDescriptor);
        services.AddScoped(_ => new Mock<INotificationService>().Object);

        var constraintDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUniqueConstraintChecker));
        if (constraintDescriptor != null) services.Remove(constraintDescriptor);
        services.AddScoped(_ => new Mock<IUniqueConstraintChecker>().Object);
    }

    private static void RemoveBackgroundServices(IServiceCollection services)
    {
        var hostedServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
        foreach (var hs in hostedServices)
        {
            if (hs.ImplementationType?.Name is "PaymentTimeoutBackgroundService"
                or "PointsExpiryBackgroundService"
                or "DeliveredOrdersCompletionBackgroundService")
            {
                services.Remove(hs);
            }
        }
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var result = await response.Content.ReadFromJsonAsync<Presentation.Common.Responses.ApiResponse<Application.DTOs.Auth.AuthResponse>>();
        return result?.Data?.Token ?? string.Empty;
    }

    public HttpClient CreateAuthenticatedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
