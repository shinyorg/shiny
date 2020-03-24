#if WINDOWS_UWP || __ANDROID__
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Notifications
{
    public class NotificationJob : IJob
    {
        readonly IRepository repository;
        readonly INotificationManager manager;


        public NotificationJob(INotificationManager manager, IRepository repository)
        {
            this.manager = manager;
            this.repository = repository;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            var all = await this.repository.GetAll<Notification>();
            var pending = all
                .Where(x =>
                    x.ScheduleDate != null &&
                    x.ScheduleDate.Value < DateTimeOffset.UtcNow
                )
                .ToList();

            var anyPending = false;
            foreach (var notification in pending)
            {
                anyPending = true;
                notification.ScheduleDate = null; // slight hack, kill schedule date as we're triggering now
                await this.manager.Send(notification);
                await this.repository.Remove<Notification>(notification.Id.ToString());
            }
            return anyPending;
        }
    }
}
#endif