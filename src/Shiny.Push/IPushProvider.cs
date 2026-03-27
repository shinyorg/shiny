using System.Threading.Tasks;

namespace Shiny.Push;


/// <summary>
/// Provides platform-specific push notification registration with a third-party provider
/// </summary>
public interface IPushProvider
{

#if ANDROID
    /// <summary>
    /// Registers the native push token with the push provider
    /// </summary>
    /// <param name="nativeToken">The FCM token</param>
    /// <returns>The provider-specific registration token</returns>
    Task<string> Register(string nativeToken);

    /// <summary>
    /// Unregisters from the push provider
    /// </summary>
    Task UnRegister();
#elif APPLE
    /// <summary>
    /// Registers the native push token with the push provider
    /// </summary>
    /// <param name="nativeToken">The APNs device token</param>
    /// <returns>The provider-specific registration token</returns>
    Task<string> Register(Foundation.NSData nativeToken);

    /// <summary>
    /// Unregisters from the push provider
    /// </summary>
    Task UnRegister();
#endif
}

