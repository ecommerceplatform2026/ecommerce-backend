using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Ecommerce.PerformanceTests;

public sealed class PerformanceTestContainers : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;

    public string PostgresConnectionString => _postgres?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container not started");
    public string RedisConnectionString => _redis?.GetConnectionString()
        ?? throw new InvalidOperationException("Redis container not started");

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("ecommerce_perftest")
            .WithUsername("perftest")
            .WithPassword("perftest123")
            .Build();

        _redis = new RedisBuilder()
            .WithImage("redis:7")
            .Build();

        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            (_postgres?.DisposeAsync().AsTask() ?? Task.CompletedTask),
            (_redis?.DisposeAsync().AsTask() ?? Task.CompletedTask));
    }
}
