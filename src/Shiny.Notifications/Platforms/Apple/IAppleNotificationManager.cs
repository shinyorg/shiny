using System.Threading.Tasks;
using UserNotifications;

namespace Shiny.Notifications;


public interface IAppleNotificationManager : INotificationManager
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="authOptions">Defaults to Banner | Badge | Sound</param>
    /// <returns></returns>
    Task RequestAccess(UNAuthorizationOptions authOptions);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="notification"></param>
    /// <returns></returns>
    Task Send(AppleNotification notification);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    void AddChannel(AppleChannel channel);
}
