using System;
using System.Threading.Tasks;
using UserNotifications;

namespace Shiny.Notifications;


public class AppleNotification : Notification
{
    public Func<Channel, UNNotificationRequest, Task>? Customize { get; }
}
