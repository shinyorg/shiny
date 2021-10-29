using System;
using System.Collections.Generic;
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
                .Android
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
        }


        public Task Clear()
        {
            this.manager.NativeManager.CancelAll();
            return this.core.Repository.Clear<Notification>();
        }


        public async Task<IEnumerable<Notification>> GetPending()
            => await this.core.Repository.GetAll<Notification>().ConfigureAwait(false);


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

            if (notification.ScheduleDate != null)
            {
                await this.core.Repository.Set(notification.Id.ToString(), notification);
            }
            else
            {
                this.manager.SendNative(notification.Id, builder.Build());
                //if (notification.BadgeCount != null)
                //    this.Services.SetBadgeCount(notification.BadgeCount.Value);
            }
        }


        public int Badge
        {
            get => this.core.GetBadgeCount();
            set => this.core.SetBadgeCount(value);
        }
    }
}
