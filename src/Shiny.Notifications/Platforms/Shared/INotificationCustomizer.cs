using System.Threading.Tasks;

namespace Shiny.Notifications;


#if ANDROID
public interface INotificationCustomizer
{
    Task Customize(Notification notification, Channel channel, Android.App.Notification nativeNotification);
}
#elif IOS
public interface INotificationCustomizer
{
    Task Customize(Notification notification, Channel channel, UserNotifications.UNNotification nativeNotification);
}

#endif