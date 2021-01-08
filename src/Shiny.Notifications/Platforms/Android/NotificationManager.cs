using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Android.Graphics;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Notifications
{
    public class NotificationManager : INotificationManager, IPersistentNotificationManagerExtension
    {
        readonly ShinyCoreServices services;
        readonly NotificationManagerCompat manager;


        public NotificationManager(ShinyCoreServices services)
        {
            this.services = services;
            this.manager = NotificationManagerCompat.From(this.services.Android.AppContext);
            this.services
                .Android
                .WhenIntentReceived()
                .Subscribe(x => this
                    .services
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


        public IPersistentNotification Create(Notification notification)
        {
            notification.Android.OnGoing = true;
            notification.Android.ShowWhen = null;
            notification.ScheduleDate = null;

            var builder = this.CreateNativeBuilder(notification);
            var pnotification = new AndroidPersistentNotification(notification.Id, this.manager, builder);

            this.manager.Notify(notification.Id, builder.Build());
            return pnotification;
        }


        public Android.App.Notification CreateNativeNotification(Notification notification) =>
            this.CreateNativeBuilder(notification).Build();


        public async Task Cancel(int id)
        {
            this.manager.Cancel(id);
            await this.services.Repository.Remove<Notification>(id.ToString());
        }


        public async Task Clear()
        {
            this.manager.CancelAll();
            await this.services.Repository.Clear<Notification>();
        }


        public async Task<IEnumerable<Notification>> GetPending()
            => await this.services.Repository.GetAll<Notification>();


        public async Task<AccessState> RequestAccess()
        {
            var state = AccessState.Disabled;

            if (this.manager.AreNotificationsEnabled())
                state = await this.services.Jobs.RequestAccess();

            return state;
        }


        public async Task Send(Notification notification)
        {
            // this is here to cause validation of the settings before firing or scheduling
            var builder = this.CreateNativeBuilder(notification);

            if (notification.ScheduleDate != null)
            {
                await this.services.Repository.Set(notification.Id.ToString(), notification);
                return;
            }
            await this.TryApplyChannel(notification, builder);
            this.SendNative(notification.Id, builder.Build());
            await this.services.Services.SafeResolveAndExecute<INotificationDelegate>(x => x.OnReceived(notification), false);
        }


        public int Badge { get; set; }


        public virtual NotificationCompat.Builder CreateNativeBuilder(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.services.Settings.IncrementValue("NotificationId");

            var pendingIntent = this.GetLaunchPendingIntent(notification);
            var builder = new NotificationCompat.Builder(this.services.Android.AppContext)
                .SetContentTitle(notification.Title)
                .SetSmallIcon(this.GetSmallIconResource(notification))
                .SetAutoCancel(notification.Android.AutoCancel)
                .SetOngoing(notification.Android.OnGoing)
                .SetContentIntent(pendingIntent);

            if (!notification.Android.ContentInfo.IsEmpty())
                builder.SetContentInfo(notification.Android.ContentInfo);

            if (!notification.Android.Ticker.IsEmpty())
                builder.SetTicker(notification.Android.Ticker);

            if (notification.Android.UseBigTextStyle)
                builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(notification.Message));
            else
                builder.SetContentText(notification.Message);

            this.TrySetLargeIconResource(notification, builder);

            if (notification.BadgeCount != null)
                builder.SetNumber(notification.BadgeCount.Value);

            if (!notification.Android.ColorResourceName.IsEmpty())
            {
                if (this.services.Android.IsMinApiLevel(21))
                {
                    var color = this.GetColor(notification.Android.ColorResourceName);
                    builder.SetColor(color);
                }
                else
                {
                    Log.Write(NotificationLogCategory.Notifications, "ColorResourceName is only supported on API 21+");
                }
            }

            if (notification.Android.ShowWhen != null)
                builder.SetShowWhen(notification.Android.ShowWhen.Value);

            if (notification.Android.When != null)
                builder.SetWhen(notification.Android.When.Value.ToUnixTimeMilliseconds());

            return builder;
        }


        public virtual void SendNative(int id, Android.App.Notification notification)
            => this.manager.Notify(id, notification);


        public async Task SetChannels(params Channel[] channels)
        {
            var existing = await this.services.Repository.GetChannels();
            foreach (var exist in existing)
                this.manager.DeleteNotificationChannel(exist.Identifier);

            await this.services.Repository.DeleteAllChannels();
            foreach (var channel in channels)
                await this.CreateChannel(channel);
        }


        public Task<IList<Channel>> GetChannels()
            => this.services.Repository.GetChannels();


        protected virtual void CreateNativeChannel(Channel channel)
        {
            var native = new NotificationChannel(
                channel.Identifier,
                channel.Description ?? channel.Identifier,
                channel.Importance.ToNative()
            );
            var attrBuilder = new AudioAttributes.Builder();

            Android.Net.Uri uri = null;
            if (!channel.CustomSoundPath.IsEmpty())
                uri = this.GetSoundResourceUri(channel.CustomSoundPath);

            switch (channel.Importance)
            {
                case ChannelImportance.Critical:
                    attrBuilder
                        .SetUsage(AudioUsageKind.Alarm)
                        .SetFlags(AudioFlags.AudibilityEnforced);

                    uri ??= Android.Provider.Settings.System.DefaultAlarmAlertUri;
                    break;

                case ChannelImportance.High:
                    uri ??= Android.Provider.Settings.System.DefaultAlarmAlertUri;
                    break;

                case ChannelImportance.Normal:
                    uri ??= Android.Provider.Settings.System.DefaultNotificationUri;
                    break;

                case ChannelImportance.Low:
                    break;
            }
            if (uri != null)
                native.SetSound(uri, attrBuilder.Build());

            this.manager.CreateNotificationChannel(native);
        }


        protected virtual async Task CreateChannel(Channel channel)
        {
            channel.AssertValid();
            this.CreateNativeChannel(channel);

            await this.services.Repository.SetChannel(channel);
        }


        // Construct a raw resource path of the form
        // "android.resource://<PKG_NAME>/raw/<RES_NAME>", e.g.
        // "android.resource://com.shiny.sample/raw/notification"
        protected virtual Android.Net.Uri GetSoundResourceUri(string soundResourceName)
        {
            // Strip file extension and leading slash from resource name to allow users
            // to specify custom sounds like "notification.mp3" or "/raw/notification.mp3"
            soundResourceName = soundResourceName.TrimStart('/').Split('.').First();
            var resourceId = this.services.Android.GetRawResourceIdByName(soundResourceName);
            var resources = this.services.Android.AppContext.Resources;
            return new Android.Net.Uri.Builder()
                .Scheme(ContentResolver.SchemeAndroidResource)
                .Authority(resources.GetResourcePackageName(resourceId))
                .AppendPath(resources.GetResourceTypeName(resourceId))
                .AppendPath(resources.GetResourceEntryName(resourceId))
                .Build();
        }


        protected virtual PendingIntent GetLaunchPendingIntent(Notification notification, string? actionId = null)
        {
            Intent launchIntent;
            if (notification.Android?.LaunchActivityType == null)
            {
                launchIntent = this.services
                    .Android
                    .AppContext
                    .PackageManager
                    .GetLaunchIntentForPackage(this.services.Android.Package.PackageName)
                    .SetFlags(notification.Android.LaunchActivityFlags.ToNative());
            }
            else
            {
                launchIntent = new Intent(this.services.Android.AppContext, notification.Android.LaunchActivityType);
            }

            var notificationString = this.services.Serializer.Serialize(notification);
            launchIntent.PutExtra(AndroidNotificationProcessor.NOTIFICATION_KEY, notificationString);
            if (notification.Payload != null)
            {
                foreach (var item in notification.Payload)
                    launchIntent.PutExtra(item.Key, item.Value);
            }

            PendingIntent pendingIntent;
            if ((notification.Android.LaunchActivityFlags & AndroidActivityFlags.ClearTask) != 0)
            {
                pendingIntent = AndroidX.Core.App.TaskStackBuilder
                    .Create(this.services.Android.AppContext)
                    .AddNextIntent(launchIntent)
                    .GetPendingIntent(notification.Id, (int)PendingIntentFlags.OneShot);
            }
            else
            {
                pendingIntent = PendingIntent.GetActivity(
                    this.services.Android.AppContext,
                    notification.Id,
                    launchIntent,
                    PendingIntentFlags.OneShot
                );
            }
            return pendingIntent;
        }


        protected virtual int GetColor(string colorResourceName)
        {
            var colorResourceId = this.services.Android.GetColorByName(colorResourceName);
            if (colorResourceId <= 0)
                throw new ArgumentException($"Color ResourceId for {colorResourceName} not found");

            return ContextCompat.GetColor(this.services.Android.AppContext, colorResourceId);
        }


        protected virtual int GetSmallIconResource(Notification notification)
        {
            if (notification.Android.SmallIconResourceName.IsEmpty())
            {
                var id = this.services.Android.GetResourceIdByName("notification");
                if (id > 0)
                    return id;

                return this.services.Android.AppContext.ApplicationInfo.Icon;
            }
            var smallIconResourceId = this.services.Android.GetResourceIdByName(notification.Android.SmallIconResourceName);
            if (smallIconResourceId <= 0)
                throw new ArgumentException($"Icon ResourceId for {notification.Android.SmallIconResourceName} not found");

            return smallIconResourceId;
        }


        protected virtual void TrySetLargeIconResource(Notification notification, NotificationCompat.Builder builder)
        {
            if (notification.Android.LargeIconResourceName.IsEmpty())
                return;

            var iconId = this.services.Android.GetResourceIdByName(notification.Android.LargeIconResourceName);
            if (iconId > 0)
                builder.SetLargeIcon(BitmapFactory.DecodeResource(this.services.Android.AppContext.Resources, iconId));
        }


        protected virtual async Task TryApplyChannel(Notification notification, NotificationCompat.Builder builder)
        {
            var channel = Channel.Default;
            if (notification.Channel.IsEmpty())
            {
                if (this.manager.GetNotificationChannel(Channel.Default.Identifier) == null)
                    this.CreateNativeChannel(Channel.Default);
            }
            else
            {
                channel = await this.services.Repository.GetChannel(notification.Channel);
                if (channel == null)
                    throw new ArgumentException($"{notification.Channel} does not exist");
            }

            if (channel.Actions != null)
            {
                foreach (var action in channel.Actions)
                {
                    switch (action.ActionType)
                    {
                        case ChannelActionType.OpenApp:
                            break;

                        case ChannelActionType.TextReply:
                            var textReplyAction = this.CreateTextReply(notification, action);
                            builder.AddAction(textReplyAction);
                            break;

                        case ChannelActionType.None:
                        case ChannelActionType.Destructive:
                            var destAction = this.CreateAction(notification, action);
                            builder.AddAction(destAction);
                            break;

                        default:
                            throw new ArgumentException("Invalid action type");
                    }
                }
            }
        }


        static int counter = 100;
        protected virtual PendingIntent CreateActionIntent(Notification notification, ChannelAction action)
        {
            var intent = this.services.Android.CreateIntent<NotificationBroadcastReceiver>(NotificationBroadcastReceiver.IntentAction);
            var content = this.services.Serializer.Serialize(notification);
            intent
                .PutExtra("Notification", content)
                .PutExtra("Action", action.Identifier);

            counter++;
            var pendingIntent = PendingIntent.GetBroadcast(
                this.services.Android.AppContext,
                counter,
                intent,
                PendingIntentFlags.UpdateCurrent
            );
            return pendingIntent;
        }


        protected virtual NotificationCompat.Action CreateAction(Notification notification, ChannelAction action)
        {
            var pendingIntent = this.CreateActionIntent(notification, action);
            var iconId = this.services.Android.GetResourceIdByName(action.Identifier);
            var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent).Build();

            return nativeAction;
        }


        protected virtual NotificationCompat.Action CreateTextReply(Notification notification, ChannelAction action)
        {
            var pendingIntent = this.CreateActionIntent(notification, action);
            var input = new AndroidX.Core.App.RemoteInput.Builder("Result")
                .SetLabel(action.Title)
                .Build();

            var iconId = this.services.Android.GetResourceIdByName(action.Identifier);
            var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent)
                .SetAllowGeneratedReplies(true)
                .AddRemoteInput(input)
                .Build();

            return nativeAction;
        }
    }
}
