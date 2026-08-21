using System.Diagnostics.CodeAnalysis;
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
    public static IServiceCollection AddLiveActivities(this IServiceCollection services)
    {
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
    public static IServiceCollection AddLiveActivities<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(
        this IServiceCollection services
    ) where TDelegate : class, ILiveActivityDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        return services.AddLiveActivities();
    }
}
