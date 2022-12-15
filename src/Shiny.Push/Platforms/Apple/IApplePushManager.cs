using System.Threading;
using System.Threading.Tasks;
using UserNotifications;

namespace Shiny.Push;


public interface IApplePushManager : IPushManager
{
    Task<PushAccessState> RequestAccess(
        UNAuthorizationOptions options = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
        CancellationToken cancelToken = default
    );
}