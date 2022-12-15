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
