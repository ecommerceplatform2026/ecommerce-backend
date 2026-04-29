using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces.Services;
using Infrastructure.Services;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Base;
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

            return services;
        }
    }
}
