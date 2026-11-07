using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE.AccessorySetup;

namespace Shiny;


/// <summary>Registers Shiny.BluetoothLE.AccessorySetup.</summary>
public static class AccessorySetupServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IAccessorySetupManager"/> on iOS and Android, and Shiny.BluetoothLE's <c>IBleManager</c> (through
    /// <c>AddBluetoothLE()</c>) to talk to the accessories once they are picked. Elsewhere only Bluetooth LE is
    /// registered - check for the accessory setup service. Calling <c>AddBluetoothLE</c> yourself, before or after,
    /// is fine: a delegate or Apple configuration you pass it is kept.
    /// </summary>
    public static IServiceCollection AddAccessorySetup(this IServiceCollection services)
    {
        services.AddBluetoothLE();
#if IOS || ANDROID
        if (!services.HasImplementation<AccessorySetupManager>())
            services.AddSingletonAsImplementedInterfaces<AccessorySetupManager>();
#endif
        return services;
    }


    /// <summary>
    /// Adds <see cref="IAccessorySetupManager"/> and a delegate for accessories being added, removed or changed
    /// outside the app's own calls, and (Android) coming into or leaving range. Call it once per delegate.
    /// </summary>
    public static IServiceCollection AddAccessorySetup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services)
        where TDelegate : class, IAccessorySetupDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        return services.AddAccessorySetup();
    }
}
