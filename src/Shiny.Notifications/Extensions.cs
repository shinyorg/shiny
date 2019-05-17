using System;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public static class Extensions
    {
        public static Task Send(this INotificationManager notifications, string title, string message)
            => notifications.Send(new Notification
            {
                Title = title,
                Message = message
            });
    }
}
