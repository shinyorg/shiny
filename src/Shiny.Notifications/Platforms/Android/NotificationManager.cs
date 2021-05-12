using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public partial class NotificationManager : INotificationManager
    {
        readonly ShinyCoreServices core;
        readonly AndroidNotificationManager manager;


        public NotificationManager(ShinyCoreServices core, AndroidNotificationManager manager)
        {
            this.core = core;
            this.manager = manager;

            this.core
                .Android
                .WhenIntentReceived()
                .Subscribe(x => this
                    .core
                    .Services
                    .Resolve<AndroidNotificationProcessor>()
                    .TryProcessIntent(x)
                );

            // auto process intent?
            //this.context
            //    .WhenActivityStatusChanged()
            //    .Where(x => x.Status == ActivityState.Created)
            //    .Subscribe(x => TryProcessIntent(x.Activity.Intent));
        }


        public async Task Cancel(int id)
        {
            this.manager.NativeManager.Cancel(id);
            await this.core.Repository.Remove<Notification>(id.ToString());
        }


        public async Task Clear()
        {
            this.manager.NativeManager.CancelAll();
            await this.core.Repository.Clear<Notification>();
        }


        public async Task<IEnumerable<Notification>> GetPending()
            => await this.core.Repository.GetAll<Notification>();


        public async Task<AccessState> RequestAccess()
        {
            if (!this.manager.NativeManager.AreNotificationsEnabled())
                return AccessState.Disabled;

            var result = await this.core.Jobs.RequestAccess();
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
                this.manager.SendNative(notification.Id, builder.Build());
            else
                await this.core.Repository.Set(notification.Id.ToString(), notification);
        }


        public int Badge
        {
            get => this.core.GetBadgeCount();
            set => this.core.SetBadgeCount(value);
        }
    }
}
