using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces.Services;
using Infrastructure.Services;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
using Infrastructure.Repositories.Base;

namespace Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
            services.AddScoped<IUniqueConstraintChecker, PostgresUniqueConstraintChecker>();
            services.AddHttpClient<IProductImageStorage, CloudinaryProductImageStorage>();

            return services;
        }
    }
}
