//#if WINDOWS_UWP || __ANDROID__
//using System;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Shiny.Infrastructure;
//using Shiny.Jobs;


//namespace Acr.Notifications
//{
//    public class NotificationJob : IJob
//    {
//        readonly IRepository repository;
//        readonly INotificationManager manager;


//        public NotificationJob(INotificationManager manager, IRepository repository)
//        {
//            this.manager = manager;
//            this.repository = repository;
//        }


//        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
//        {
//            var all = await this.repository.GetAll<Notification>();
//            var pending = all.Where(x => x.NextTriggerDate < DateTime.UtcNow);

//            foreach (var notification in pending)
//                await this.Process(notification);
//        }


//        async Task Process(Notification notification)
//        {
//            var dt = notification.NextTriggerDate.Value;
//            notification.NextTriggerDate = null; // somewhat hacky way of shoving this back through the system
//            await this.manager.Send(notification);

//            switch (notification.RepeatInterval)
//            {
//                case NotificationRepeatInterval.None:
//                    break;

//                case NotificationRepeatInterval.Daily:
//                    notification.NextTriggerDate = dt.AddDays(1);
//                    break;

//                case NotificationRepeatInterval.Monthly:
//                    notification.NextTriggerDate = dt.AddMonths(1);
//                    break;

//                case NotificationRepeatInterval.Weekly:
//                    notification.NextTriggerDate = dt.AddDays(7);
//                    break;

//                case NotificationRepeatInterval.Yearly:
//                    notification.NextTriggerDate = dt.AddYears(1);
//                    break;
//            }

//            if (notification.NextTriggerDate == null)
//                await this.repository.Remove<Notification>(notification.Id.ToString());
//            else
//                await this.repository.Set(notification.Id.ToString(), notification);
//        }
//    }
//}
//#endif


//#if WINDOWS_UWP || __ANDROID__
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Shiny.Infrastructure;
//using Shiny.Jobs;


//namespace Acr.Notifications
//{
//    public abstract class AbstractPlatformNotificationManagerImpl : AbstractNotificationManagerImpl
//    {
//        protected AbstractPlatformNotificationManagerImpl(IRepository repository, IJobManager jobManager)
//        {
//            this.Repository = repository;
//            this.Jobs = jobManager;

//            this.Jobs.Schedule(new JobInfo
//            {
//                Identifier = nameof(NotificationJob),
//                Type = typeof(NotificationJob)
//            });
//        }


//        protected IRepository Repository { get; }
//        protected IJobManager Jobs { get;}

//        public override Task<IEnumerable<Notification>> GetPendingNotifications() => this.Repository.GetPendingNotifications();
//        public override Task CancelAll() => this.Repository.Clear<Notification>();
//        public override Task Cancel(int notificationId) => this.Repository.DeleteNotification(notificationId);
//    }
//}
//#endif