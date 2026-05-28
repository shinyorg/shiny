using System.Threading;
using System.Threading.Tasks;
using UserNotifications;

namespace Shiny.Push;


/// <summary>
/// Extends <see cref="IPushManager"/> with Apple-specific push notification authorization options.
/// </summary>
public interface IApplePushManager : IPushManager
{
    /// <summary>
    /// Requests push notification access with specific Apple notification authorization options.
    /// </summary>
    /// <param name="options">The UNAuthorizationOptions to request.</param>
    /// <param name="cancelToken">Cancellation token.</param>
    /// <returns>The push access state including the device token.</returns>
    Task<PushAccessState> RequestAccess(
        UNAuthorizationOptions options = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
        CancellationToken cancelToken = default
    );
}
