using System;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public partial class NotificationManager : IPersistentNotificationManagerExtension
    {
        public async Task<IPersistentNotification> Create(Notification notification, bool show)
        {
            var channel = await this.GetChannel(notification);

            notification.Android.OnGoing = true;
            notification.Android.ShowWhen = null;
            notification.ScheduleDate = null;

            var builder = this.manager.CreateNativeBuilder(notification, channel);
            var p = new AndroidPersistentNotification(this.manager, builder);
            if (show)
                p.Show();

            return p;
        }
    }
}
