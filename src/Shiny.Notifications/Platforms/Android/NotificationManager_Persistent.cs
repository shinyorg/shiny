using System;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public partial class NotificationManager : IPersistentNotificationManagerExtension
    {
        public async Task<IPersistentNotification> Create(Notification notification)
        {
            var channel = await this.core.Repository.GetChannel(notification.Channel ?? Channel.Default.Identifier);

            notification.Android.OnGoing = true;
            notification.Android.ShowWhen = null;
            notification.ScheduleDate = null;

            var builder = this.manager.CreateNativeBuilder(notification, channel);
            var pnotification = new AndroidPersistentNotification(notification.Id, this.manager.NativeManager, builder);

            this.manager.NativeManager.Notify(notification.Id, builder.Build());
            return pnotification;
        }
    }
}
