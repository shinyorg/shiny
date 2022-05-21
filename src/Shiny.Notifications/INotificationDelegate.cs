using System.Threading.Tasks;

namespace Shiny.Notifications;


public interface INotificationDelegate
{
    ///// <summary>
    ///// Does not fire on scheduled for iOS
    ///// </summary>
    ///// <param name="notification"></param>
    ///// <returns></returns>
    //Task OnSent(Notification notification);

    /// <summary>
    /// This will fire when the user taps on a notification (or responds using a command)
    /// </summary>
    /// <param name="response"></param>
    Task OnEntry(NotificationResponse response);
}
