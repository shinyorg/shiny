using System;
using System.Threading.Tasks;
using Android.Content;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public class AlarmBroadcastReceiver : ShinyBroadcastReceiver
    {
        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            var notificationId = intent.GetIntExtra("NotificationId", 0);
            if (notificationId > 0)
            {
                var repo = this.Resolve<IRepository>();
                var notification = await repo.Get<Notification>(notificationId.ToString());
                notification.ScheduleDate = null;
                await this.Resolve<INotificationManager>().Send(notification);
                await repo.Remove<Notification>(notificationId.ToString());
            }
        }
    }
}
