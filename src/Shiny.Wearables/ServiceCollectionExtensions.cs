using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Hosting;
using Shiny.Wearables;

namespace Shiny;


/// <summary>Registers Shiny.Wearables.</summary>
public static class WearablesServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IWearableManager"/> on iOS and Android. Elsewhere nothing is registered — check for the
    /// service — and everything the wearable sends is ignored.
    /// </summary>
    public static IServiceCollection AddWearables(this IServiceCollection services)
    {
#if IOS || ANDROID
        services.AddSingletonAsImplementedInterfaces<WearableManager>();
#endif
        return services;
    }


    /// <summary>
    /// Adds <see cref="IWearableManager"/> and a delegate for what the wearable sends. Call it once per delegate;
    /// every registered delegate runs.
    /// </summary>
    public static IServiceCollection AddWearables<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services)
        where TDelegate : class, IWearableDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        return services.AddWearables();
    }
}
