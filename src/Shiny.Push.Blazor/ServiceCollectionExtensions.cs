using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;
using Shiny.Push.Blazor;

namespace Shiny;


public static class BlazorPushServiceCollectionExtensions
{
    /// <summary>
    /// Adds Web Push notification support to a Blazor PWA. Requires a VAPID public key.
    /// </summary>
    public static IServiceCollection AddPush(this IServiceCollection services, WebPushOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<PushManager>();
        services.AddSingleton<IPushManager>(sp => sp.GetRequiredService<PushManager>());
        services.AddLocalStorageKeyValueStore();
        return services;
    }


    /// <summary>
    /// Adds Web Push notification support with a custom <see cref="IPushDelegate"/>.
    /// </summary>
    public static IServiceCollection AddPush<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDelegate>(this IServiceCollection services, WebPushOptions options)
        where TDelegate : class, IPushDelegate
    {
        services.AddSingleton<TDelegate>();
        services.AddSingleton<IPushDelegate>(sp => sp.GetRequiredService<TDelegate>());
        return services.AddPush(options);
    }
}
