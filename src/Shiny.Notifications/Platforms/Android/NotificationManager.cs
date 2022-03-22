using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Locations;


namespace Shiny.Notifications
{
    public partial class NotificationManager : INotificationManager
    {
        readonly ShinyCoreServices core;
        readonly AndroidNotificationManager manager;
        readonly IGeofenceManager geofenceManager;


        public NotificationManager(ShinyCoreServices core,
                                   AndroidNotificationManager manager,
                                   IGeofenceManager geofenceManager)
        {
            this.core = core;
            this.manager = manager;
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
            // TODO: await this.SetNotificationJob();
        }


        public async Task Clear()
        {
            // TODO: need to kill any associated geofences
            this.manager.NativeManager.CancelAll();
            await this.core.Repository.Clear<Notification>();
            // TODO: await this.CancelJob();
        }


        public async Task<IEnumerable<Notification>> GetPending()
            => await this.core.Repository.GetList<Notification>().ConfigureAwait(false);


        public async Task<AccessState> RequestAccess(bool locationAware)
        {
            if (!this.manager.NativeManager.AreNotificationsEnabled())
                return AccessState.Disabled;

            if (locationAware)
            {
                var locPermission = await this.geofenceManager.RequestAccess();
                if (locPermission != AccessState.Available)
                    return AccessState.Restricted;
            }

            return AccessState.Available;
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

            if (notification.Geofence != null)
            {
                await this.geofenceManager.StartMonitoring(new GeofenceRegion(
                    NotificationGeofenceDelegate.GetGeofenceId(notification),
                    notification.Geofence!.Center!,
                    notification.Geofence!.Radius!
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
                // TODO: await this.EnsureStartJob();
            }
        }



        public int Badge
        {
            get => this.core.GetBadgeCount();
            set => this.core.SetBadgeCount(value);
        }
    }
}
