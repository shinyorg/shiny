using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Locations;


namespace Shiny.Notifications
{
    public partial class NotificationManager : INotificationManager
    {
        readonly ShinyCoreServices core;
        readonly AndroidNotificationManager manager;
        readonly IGeofenceManager geofenceManager;
        readonly IJobManager jobManager;


        public NotificationManager(ShinyCoreServices core,
                                   AndroidNotificationManager manager,
                                   IGeofenceManager geofenceManager,
                                   IJobManager jobManager)
        {
            this.core = core;
            this.manager = manager;
            this.jobManager = jobManager;
            this.geofenceManager = geofenceManager;

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
            var notification = await this.core.Repository.Get<Notification>(id.ToString());
            if (notification != null)
            {
                if (notification.Geofence != null)
                {
                    var geofenceId = NotificationGeofenceDelegate.GetGeofenceId(notification);
                    await this.geofenceManager.StopMonitoring(geofenceId);
                }
                await this.core.Repository.Remove<Notification>(id.ToString());
            }
            await this.SetNotificationJob();
        }


        public async Task Clear()
        {
            // TODO: need to kill any associated geofences
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

            if (notification.ScheduleDate != null && notification.Geofence != null)
                throw new InvalidOperationException("You cannot have a schedule date and geofence on the same notification");

            // TODO: user should check geofence permissions before sending any geofence repos
            if (notification.Geofence != null)
            {
                var result = await this.geofenceManager.RequestAccess();
                if (result != AccessState.Available)
                    throw new InvalidOperationException("User did not grant location permission for geofence based notification - " + result);

                await this.geofenceManager.StartMonitoring(new GeofenceRegion(
                    NotificationGeofenceDelegate.GetGeofenceId(notification),
                    notification.Geofence.Center,
                    notification.Geofence.Radius
                ));
            }

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
