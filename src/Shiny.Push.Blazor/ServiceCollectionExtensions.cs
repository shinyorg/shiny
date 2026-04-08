using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;
using Shiny.Push.Blazor;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Web Push notification support to a Blazor PWA. Requires a VAPID public key.
    /// </summary>
    public static IServiceCollection AddPush(this IServiceCollection services, WebPushOptions options)
    {
        services.AddSingleton(options);
        services.AddShinyService<PushManager>();
        services.AddLocalStorageKeyValueStore();
        return services;
    }


    /// <summary>
    /// Adds Web Push notification support with a custom <see cref="IPushDelegate"/>.
    /// </summary>
    public static IServiceCollection AddPush<TDelegate>(this IServiceCollection services, WebPushOptions options)
        where TDelegate : class, IPushDelegate
    {
        services.AddShinyService<TDelegate>();
        return services.AddPush(options);
    }
}
