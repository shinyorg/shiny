using System;
using UserNotifications;

namespace Shiny.Notifications;


public interface IAppleNotificationDelegate : INotificationDelegate
{
    UNNotificationPresentationOptions? GetForegroundPresentation(Notification notification);
}