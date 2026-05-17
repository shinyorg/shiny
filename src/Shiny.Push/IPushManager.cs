using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Push;


/// <summary>
/// Manages push notification registration, permission, and provider-issued tokens for the current device.
/// </summary>
public interface IPushManager
{
    /// <summary>
    /// Tag support for the current registration, or null if the active push provider does not support tags.
    /// </summary>
    IPushTagSupport? Tags { get; }

    /// <summary>
    /// The current registration token issued by the push provider (e.g. Azure Notification Hubs install id or FCM token).
    /// </summary>
    string? RegistrationToken { get; }

    /// <summary>
    /// The raw native push token from the operating system (APNs/FCM/WNS). Intended for debugging; use <see cref="RegistrationToken"/> for sending pushes.
    /// </summary>
    string? NativeRegistrationToken { get; }

    /// <summary>
    /// Returns the current push notification permission state without prompting the user.
    /// </summary>
    /// <returns>The current access state.</returns>
    Task<AccessState> GetCurrentAccess();

    /// <summary>
    /// Prompts the user (when needed) for push permission and registers with the native push service and provider.
    /// </summary>
    /// <param name="cancelToken">Cancels the request.</param>
    /// <returns>The resulting access state and registration token.</returns>
    Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default);

    /// <summary>
    /// Unregisters this device from push notifications and clears the registration token.
    /// </summary>
    Task UnRegister();
}
