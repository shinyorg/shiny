using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Push;


public interface IPushManager
{
    /// <summary>
    /// Ability to tag current registration - null if not supported by provider
    /// </summary>
    IPushTagSupport? Tags { get; }

    /// <summary>
    /// The current registration token
    /// </summary>
    string? RegistrationToken { get; }

    /// <summary>
    /// This is from the OS and does not necessarily represent the registration token with
    /// your push provider.  You should use RegistrationToken for everything else - this is for debugging
    /// </summary>
    string? NativeRegistrationToken { get; }

    /// <summary>
    /// Gets the current push notification access state without prompting the user
    /// </summary>
    /// <returns></returns>
    Task<AccessState> GetCurrentAccess();

    /// <summary>
    /// Requests platform permission to send push notifications
    /// </summary>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default);

    /// <summary>
    /// Unregisters from push notifications
    /// </summary>
    /// <returns></returns>
    Task UnRegister();
}
