using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AndroidX.Core.App;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public partial class NotificationManager : INotificationManager
    {
        readonly ShinyCoreServices core;
        readonly NotificationManagerCompat manager;


        public NotificationManager(ShinyCoreServices core)
        {
            this.core = core;
            this.manager = NotificationManagerCompat.From(this.core.Android.AppContext);
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
            this.manager.Cancel(id);
            await this.core.Repository.Remove<Notification>(id.ToString());
        }


        public async Task Clear()
        {
            this.manager.CancelAll();
            await this.core.Repository.Clear<Notification>();
        }


        public async Task<IEnumerable<Notification>> GetPending()
            => await this.core.Repository.GetAll<Notification>();


        public async Task<AccessState> RequestAccess()
        {
            if (!this.manager.AreNotificationsEnabled())
                return AccessState.Disabled;

            var result = await this.core.Jobs.RequestAccess();
            return result;
        }


        public async Task Send(Notification notification)
        {
            // this is here to cause validation of the settings before firing or scheduling
            var channel = await this.GetChannel(notification);
            var builder = this.CreateNativeBuilder(notification, channel);
            
            if (notification.ScheduleDate != null)
            {
                await this.core.Repository.Set(notification.Id.ToString(), notification);
                //this.SetAlarm(notification);
                return;
            }
            this.SendNative(notification.Id, builder.Build());
            await this.core
                .Services
                .SafeResolveAndExecute<INotificationDelegate>(
                    x => x.OnReceived(notification),
                    false
                );
        }


        public int Badge { get; set; }
    }
}
