using System.Threading;
using System.Threading.Tasks;
using UserNotifications;

namespace Shiny.Push;


/// <summary>
/// Extends IPushManager with Apple-specific push notification authorization options
/// </summary>
public interface IApplePushManager : IPushManager
{
    /// <summary>
    /// Requests push notification access with specific Apple notification authorization options
    /// </summary>
    Task<PushAccessState> RequestAccess(
        UNAuthorizationOptions options = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
        CancellationToken cancelToken = default
    );
}
