using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.LiveActivities;

namespace Shiny;


/// <summary>Registers live activity services with the Shiny host.</summary>
public static class LiveActivitiesServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ILiveActivityManager"/>. On iOS/Android this also hooks app startup so
    /// push tokens and lifecycle changes are observed from launch; elsewhere a no-op implementation is
    /// registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// Optional configuration. Android-only today - it localizes the notification channel's name and
    /// description, which are otherwise hard-coded English.
    /// </param>
    public static IServiceCollection AddLiveActivities(
        this IServiceCollection services,
        Action<LiveActivityOptions>? configure = null
    )
    {
        // A caller that configures nothing must not overwrite options someone else already set - the
        // iOS transfer progress renderer calls this itself when the app has not
        if (configure != null || services.All(x => x.ServiceType != typeof(LiveActivityOptions)))
        {
            var options = new LiveActivityOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
        }

#if PLATFORM
        services.AddSingletonAsImplementedInterfaces<LiveActivityManager>();
#else
        services.AddSingleton<ILiveActivityManager, NoOpLiveActivityManager>();
#endif
        return services;
    }


    /// <summary>
    /// Registers <see cref="ILiveActivityManager"/> along with a delegate that receives lifecycle and
    /// push token callbacks. Register this if you push activity updates from a server — the two token
    /// callbacks are the only way to learn the tokens.
    /// </summary>
    /// <typeparam name="TDelegate">Your <see cref="ILiveActivityDelegate"/> implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration - see the non-generic overload.</param>
    public static IServiceCollection AddLiveActivities<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(
        this IServiceCollection services,
        Action<LiveActivityOptions>? configure = null
    ) where TDelegate : class, ILiveActivityDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        return services.AddLiveActivities(configure);
    }
}
