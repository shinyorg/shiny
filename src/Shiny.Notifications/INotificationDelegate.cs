using System.Threading.Tasks;

namespace Shiny.Notifications;


/// <summary>
/// Application-supplied delegate that receives notification interaction callbacks. Register a single implementation with the Shiny host.
/// </summary>
public interface INotificationDelegate
{
    ///// <summary>
    ///// Does not fire on scheduled for iOS
    ///// </summary>
    ///// <param name="notification"></param>
    ///// <returns></returns>
    //Task OnSent(Notification notification);

    /// <summary>
    /// Invoked when the user taps a notification or responds via an action (including text reply input).
    /// </summary>
    /// <param name="response">The interaction response containing the originating notification, the action identifier, and any reply text.</param>
    Task OnEntry(NotificationResponse response);
}
