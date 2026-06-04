using Application.Interfaces.Events;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Base;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Linq;
using VNPAY.Extensions;

namespace Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<EcommerceContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null)));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
            services.AddScoped<IUniqueConstraintChecker, PostgresUniqueConstraintChecker>();
            services.AddHttpClient<IProductImageStorage, CloudinaryProductImageStorage>()
                    .AddStandardResilienceHandler();

            var redisConnectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException("Redis connection string 'Redis' is not configured or is empty in appsettings.");
            }

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 3000;
                return ConnectionMultiplexer.Connect(options);
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });

            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<INotificationService, NotificationService>();

            services.AddVnpayClient(config =>
            {
                var vnPaySection = configuration.GetSection("VnPay");
                config.TmnCode = vnPaySection["TmnCode"]!;
                config.HashSecret = vnPaySection["HashSecret"]!;
                config.CallbackUrl = vnPaySection["ReturnUrl"]!;
                config.BaseUrl = vnPaySection["PaymentUrl"]!;
            });

            services.AddScoped<IVnPayService, VnPayService>();
            services.AddHostedService<PaymentTimeoutBackgroundService>();

            // Domain Event Publisher & Dynamic Handlers Scanning
            services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
            
            var handlerAssembly = typeof(IDomainEventHandler<>).Assembly;
            var handlerTypes = handlerAssembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));

                foreach (var @interface in interfaces)
                {
                    services.AddScoped(@interface, handlerType);
                }
            }

            return services;
        }
    }
}
