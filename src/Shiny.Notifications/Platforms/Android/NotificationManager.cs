using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Notifications
{
    public partial class NotificationManager : INotificationManager
    {
        readonly ShinyCoreServices core;
        readonly AndroidNotificationManager manager;
        readonly IJobManager jobManager;


        public NotificationManager(ShinyCoreServices core,
                                   AndroidNotificationManager manager,
                                   IJobManager jobManager)
        {
            this.core = core;
            this.manager = manager;
            this.jobManager = jobManager;

            this.core
                .Platform
                .WhenIntentReceived()
                .SubscribeAsync(x => this
                    .core
                    .Services
                    .Resolve<AndroidNotificationProcessor>()!
                    .TryProcessIntent(x)
                );
        }


        public async Task Cancel(int id)
        {
            this.manager.NativeManager.Cancel(id);
            await this.core.Repository.Remove<Notification>(id.ToString());
            await this.SetNotificationJob();
        }


        public async Task Clear()
        {
            this.manager.NativeManager.CancelAll();
            await this.core.Repository.Clear<Notification>();
            await this.CancelJob();
        }


        public async Task<IEnumerable<Notification>> GetPending()
            => await this.core.Repository.GetList<Notification>().ConfigureAwait(false);


        public async Task<AccessState> RequestAccess()
        {
            if (!this.manager.NativeManager.AreNotificationsEnabled())
                return AccessState.Disabled;

            // this is only need if there is a delegate
            var result = await this.jobManager
                .RequestAccess()
                .ConfigureAwait(false);
            return result;
        }


        public async Task Send(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.core.Settings.IncrementValue("NotificationId");

            // this is here to cause validation of the settings before firing or scheduling
            var channel = await this.GetChannel(notification);
            var builder = this.manager.CreateNativeBuilder(notification, channel);

            if (notification.ScheduleDate == null)
            {
                this.manager.SendNative(notification.Id, builder.Build());
                if (notification.BadgeCount != null)
                    this.core.SetBadgeCount(notification.BadgeCount.Value);
            }
            else
            {
                await this.core.Repository.Set(notification.Id.ToString(), notification);
                await this.EnsureStartJob();
            }
        }


        async Task SetNotificationJob()
        {
            var anyScheduled = (await this.core.Repository.GetList<Notification>()).Any(x => x.ScheduleDate != null);
            if (anyScheduled)
            {
                await this.CancelJob();
            }
            else
            {
                await this.EnsureStartJob();
            }
        }


        Task CancelJob() => this.jobManager.Cancel(nameof(NotificationJob));

        Task EnsureStartJob() => this.jobManager.Register(new JobInfo(typeof(NotificationJob), nameof(NotificationJob))
        {
            RunOnForeground = true,
            IsSystemJob = true
        });


        public int Badge
        {
            get => this.core.GetBadgeCount();
            set => this.core.SetBadgeCount(value);
        }
    }
}
