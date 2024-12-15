#if IOS || ANDROID
using Microsoft.Extensions.DependencyInjection;
using Shiny.Health;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHealthIntegration(this IServiceCollection services)
    {
        services.AddShinyService<HealthService>();
        return services;
    }
}
#endif