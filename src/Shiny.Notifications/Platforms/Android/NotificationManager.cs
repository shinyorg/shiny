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
        readonly IChannelManager channelManager;
        readonly AndroidNotificationManager manager;
        readonly IGeofenceManager geofenceManager;


        public NotificationManager(
            ShinyCoreServices core,
            AndroidNotificationManager manager,
            IChannelManager channelManager,
            IGeofenceManager geofenceManager
        )
        {
            this.core = core;
            this.manager = manager;
            this.channelManager = channelManager;
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


        public Task AddChannel(Channel channel) => this.channelManager.Add(channel);
        public Task RemoveChannel(string channelId) => this.DeleteChannel(this.channelManager, channelId);
        public Task ClearChannels() => this.DeleteAllChannels(this.channelManager);
        public Task<IList<Channel>> GetChannels() => this.channelManager.GetAll();


        public async Task Cancel(int id)
        {
            var notification = await this.core.Repository.Get<Notification>(id.ToString());
            if (notification != null)
            {
                await this.CancelInternal(notification);
                await this.core.Repository.Remove<Notification>(id.ToString());
            }
        }


        public async Task Cancel(CancelScope scope = CancelScope.All)
        {
            if (scope == CancelScope.All || scope == CancelScope.DisplayedOnly)
            {
                this.manager.NativeManager.CancelAll();
            }
            if (scope == CancelScope.All || scope == CancelScope.Pending)
            {
                var notifications = await this.core.Repository.GetList<Notification>();
                foreach (var notification in notifications)
                    await this.CancelInternal(notification);

                await this.core.Repository.Clear<Notification>();
            }
        }


        public Task<Notification?> GetNotification(int notificationId)
            => this.core.Repository.Get<Notification>(notificationId.ToString());


        public async Task<IEnumerable<Notification>> GetPendingNotifications()
            => await this.core.Repository.GetList<Notification>().ConfigureAwait(false);


        public async Task<AccessState> RequestAccess(AccessRequestFlags access)
        {
            if (!this.manager.NativeManager.AreNotificationsEnabled())
                return AccessState.Disabled;

            if (access.HasFlag(AccessRequestFlags.LocationAware))
            {
                var locPermission = await this.geofenceManager.RequestAccess();
                if (locPermission != AccessState.Available)
                    return AccessState.Restricted;
            }
            if (access.HasFlag(AccessRequestFlags.TimeSensitivity) && !this.manager.Alarms.CanScheduleExactAlarms())
                return AccessState.Restricted;

            return AccessState.Available;
        }


        public async Task Send(Notification notification)
        {
            notification.AssertValid();

            // TODO: should I cancel an existing id if the user is setting it?
            if (notification.Id == 0)
                notification.Id = this.core.Settings.IncrementValue("NotificationId");

            var channel = await this.channelManager.Get(notification.Channel ?? Channel.Default.Identifier);
            if (channel == null)
                throw new InvalidProgramException("There is no default channel!!");

            var builder = this.manager.CreateNativeBuilder(notification, channel!);

            if (notification.Geofence != null)
            {
                await this.geofenceManager.StartMonitoring(new GeofenceRegion(
                    AndroidNotificationProcessor.GetGeofenceId(notification),
                    notification.Geofence!.Center!,
                    notification.Geofence!.Radius!
                ));
            }
            else if (notification.RepeatInterval != null)
            {
                // calc first date if repeating interval
                notification.ScheduleDate = notification.RepeatInterval!.CalculateNextAlarm();
            }

            if (notification.ScheduleDate == null && notification.Geofence == null)
            {
                this.manager.SendNative(notification.Id, builder.Build());
                if (notification.BadgeCount != null)
                    this.core.SetBadgeCount(notification.BadgeCount.Value);
            }
            else
            {
                // ensure a channel is set
                notification.Channel = channel!.Identifier;
                await this.core.Repository.Set(notification.Id.ToString(), notification);

                if (notification.ScheduleDate != null)
                    this.manager.SetAlarm(notification);
            }
        }


        public Task<int> GetBadge()
            => Task.FromResult(this.core.GetBadgeCount());


        public Task SetBadge(int? badge)
        {
            this.core.SetBadgeCount(badge ?? 0);
            return Task.CompletedTask;
        }


        protected virtual async Task CancelInternal(Notification notification)
        {
            if (notification.Geofence != null)
            {
                var geofenceId = AndroidNotificationProcessor.GetGeofenceId(notification);
                await this.geofenceManager.StopMonitoring(geofenceId);
            }
            if (notification.ScheduleDate != null || notification.RepeatInterval != null)
                this.manager.CancelAlarm(notification);

            this.manager.NativeManager.Cancel(notification.Id);
        }
    }
}
