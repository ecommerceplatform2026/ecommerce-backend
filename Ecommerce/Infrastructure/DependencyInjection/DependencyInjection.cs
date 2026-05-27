using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Infrastructure.Repositories.Base;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
            services.AddScoped<IUniqueConstraintChecker, PostgresUniqueConstraintChecker>();
            services.AddHttpClient<IProductImageStorage, CloudinaryProductImageStorage>();

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
            services.AddHostedService<PaymentTimeoutBackgroundService>();

            return services;
        }
    }
}
