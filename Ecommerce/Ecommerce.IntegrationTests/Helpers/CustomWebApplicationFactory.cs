using Application.Interfaces.Services;
using Application.Interfaces.Events;
using Application.Interfaces.Security;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.IntegrationTests.Helpers
{
    public class InMemoryCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, object> _cache = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out var value) && value is T typedValue)
            {
                return Task.FromResult<T?>(typedValue);
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            if (value != null)
            {
                _cache[key] = value;
            }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            foreach (var key in _cache.Keys.ToList())
            {
                if (key.StartsWith(prefix))
                {
                    _cache.TryRemove(key, out _);
                }
            }
            return Task.CompletedTask;
        }

        public async Task<T?> GetOrAddAsync<T>(
            string key,
            Func<Task<T?>> factory,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out var cached) && cached is T typedValue)
            {
                return typedValue;
            }
            var value = await factory();
            if (value != null)
            {
                _cache[key] = value;
            }
            return value;
        }
    }

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName;

        public Mock<IProductImageStorage> ProductImageStorageMock { get; } = new();
        public Mock<INotificationService> NotificationServiceMock { get; } = new();
        public Mock<IVnPayService> VnPayServiceMock { get; } = new();
        public Mock<IUniqueConstraintChecker> UniqueConstraintCheckerMock { get; } = new();

        /// <summary>
        /// Creates a factory with a unique InMemory database name to prevent data leaking between test classes.
        /// </summary>
        public CustomWebApplicationFactory() : this(null)
        {
        }

        protected CustomWebApplicationFactory(string? dbName)
        {
            _dbName = dbName ?? $"IntegrationTestsDb_{Guid.NewGuid():N}";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>

            {
                // 1. Remove DbContext registration and add InMemory DbContext with unique DB name
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EcommerceContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                services.AddDbContext<EcommerceContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                    options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                // 2. Remove Redis connection and services
                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }
                var mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
                services.AddSingleton(mockConnectionMultiplexer.Object);

                // 3. Override ICacheService with InMemoryCacheService
                var cacheServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
                if (cacheServiceDescriptor != null)
                {
                    services.Remove(cacheServiceDescriptor);
                }
                services.AddScoped<ICacheService, InMemoryCacheService>();

                // 4. Override external services with Mocks
                var imageStorageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProductImageStorage));
                if (imageStorageDescriptor != null)
                {
                    services.Remove(imageStorageDescriptor);
                }
                services.AddScoped(sp => ProductImageStorageMock.Object);

                var notificationDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(INotificationService));
                if (notificationDescriptor != null)
                {
                    services.Remove(notificationDescriptor);
                }
                services.AddScoped(sp => NotificationServiceMock.Object);

                var vnPayDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IVnPayService));
                if (vnPayDescriptor != null)
                {
                    services.Remove(vnPayDescriptor);
                }
                services.AddScoped(sp => VnPayServiceMock.Object);

                var constraintCheckerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUniqueConstraintChecker));
                if (constraintCheckerDescriptor != null)
                {
                    services.Remove(constraintCheckerDescriptor);
                }
                services.AddScoped(sp => UniqueConstraintCheckerMock.Object);

                // 5. Remove hosted service for background tasks to avoid background conflicts during tests
                var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
                foreach (var hostedService in hostedServices)
                {
                    if (hostedService.ImplementationType?.Name == "PaymentTimeoutBackgroundService")
                    {
                        services.Remove(hostedService);
                    }
                }
            });
        }
    }
}
