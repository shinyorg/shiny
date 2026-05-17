using System.Threading.Tasks;

namespace Shiny.Push;


/// <summary>
/// Bridges the operating system's native push token (APNs/FCM/WNS) to a third-party push provider such as Azure Notification Hubs.
/// </summary>
public interface IPushProvider
{

#if ANDROID
    /// <summary>
    /// Registers the native FCM token with the push provider and returns the provider-specific registration token.
    /// </summary>
    /// <param name="nativeToken">The FCM token.</param>
    /// <returns>The provider-specific registration token.</returns>
    Task<string> Register(string nativeToken);

    /// <summary>
    /// Unregisters the current device from the push provider.
    /// </summary>
    Task UnRegister();
#elif APPLE
    /// <summary>
    /// Registers the native APNs device token with the push provider and returns the provider-specific registration token.
    /// </summary>
    /// <param name="nativeToken">The APNs device token.</param>
    /// <returns>The provider-specific registration token.</returns>
    Task<string> Register(Foundation.NSData nativeToken);

    /// <summary>
    /// Unregisters the current device from the push provider.
    /// </summary>
    Task UnRegister();
#elif WINDOWS
    /// <summary>
    /// Registers the native WNS channel URI with the push provider and returns the provider-specific registration token.
    /// </summary>
    /// <param name="nativeToken">The WNS channel URI.</param>
    /// <returns>The provider-specific registration token.</returns>
    Task<string> Register(string nativeToken);

    /// <summary>
    /// Unregisters the current device from the push provider.
    /// </summary>
    Task UnRegister();
#endif
}

