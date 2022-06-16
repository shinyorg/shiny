using System.Threading.Tasks;

namespace Shiny.Notifications;


#if ANDROID
public interface INotificationCustomizer
{
    Task Customize(Notification notification, Channel channel, AndroidX.Core.App.NotificationCompat.Builder builder);
}
#elif IOS || MACCATALYST
public interface INotificationCustomizer
{
    Task Customize(Notification notification, Channel channel, UserNotifications.UNNotificationRequest notificationRequest);
}

#endif