using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.InAppPurchases;

namespace Shiny;


public static class InAppPurchaseServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IInAppPurchaseManager"/> (StoreKit 2 on iOS, Google Play Billing on Android).
    /// The platform listener starts at app launch so out-of-band purchase updates are never missed.
    /// No-op on unsupported platforms.
    /// </summary>
    public static IServiceCollection AddInAppPurchases(this IServiceCollection services)
    {
#if IOS || ANDROID
        services.TryAddSingleton<InAppPurchaseManager>();
        services.TryAddSingleton<IInAppPurchaseManager>(sp => sp.GetRequiredService<InAppPurchaseManager>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IShinyStartupTask, InAppPurchaseManager>(
            sp => sp.GetRequiredService<InAppPurchaseManager>()
        ));
#endif
        return services;
    }


    /// <summary>
    /// Registers <see cref="IInAppPurchaseManager"/> along with a delegate that receives out-of-band purchase updates.
    /// </summary>
    public static IServiceCollection AddInAppPurchases<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDelegate>(this IServiceCollection services)
        where TDelegate : class, IPurchaseDelegate
    {
        services.AddInAppPurchases();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPurchaseDelegate, TDelegate>());
        return services;
    }
}
