using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces.Services;
using Application.Services;

namespace Application.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
