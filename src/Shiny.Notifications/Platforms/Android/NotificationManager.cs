using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Android.Graphics;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Logging;
using Shiny.Settings;


namespace Shiny.Notifications
{
    public class NotificationManager : INotificationManager, IPersistentNotificationManagerExtension
    {
        readonly AndroidContext context;
        readonly IServiceProvider services;
        readonly IRepository repository;
        readonly ISettings settings;
        readonly ISerializer serializer;
        readonly IJobManager jobs;
        readonly NotificationManagerCompat manager;


        public NotificationManager(AndroidContext context,
                                   IServiceProvider services,
                                   ISerializer serializer,
                                   IJobManager jobs,
                                   IRepository repository,
                                   ISettings settings)
        {
            this.context = context;
            this.services = services;
            this.serializer = serializer;
            this.jobs = jobs;
            this.repository = repository;
            this.settings = settings;

            this.manager = NotificationManagerCompat.From(this.context.AppContext);
            this.context
                .WhenIntentReceived()
                .Subscribe(x => this
                    .services
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


        public async Task Cancel(int id)
        {
            this.manager.Cancel(id);
            await this.repository.Remove<Notification>(id.ToString());
        }


        public async Task Clear()
        {
            this.manager.CancelAll();
            await this.repository.Clear<Notification>();
        }


        public async Task<IEnumerable<Notification>> GetPending()
            => await this.repository.GetAll<Notification>();


        public async Task<AccessState> RequestAccess()
        {
            var state = AccessState.Available;

            if (this.manager.AreNotificationsEnabled())
                state = await this.jobs.RequestAccess();
            else
                state = AccessState.Disabled;

            return state;
        }


        public async Task Send(Notification notification)
        {
            var native = this.CreateNativeNotification(notification);

            if (notification.ScheduleDate != null)
            {
                await this.repository.Set(notification.Id.ToString(), notification);
                return;
            }
            this.SendNative(notification.Id, native);
            await this.services.SafeResolveAndExecute<INotificationDelegate>(x => x.OnReceived(notification), false);
        }


        public int Badge { get; set; }

        readonly List<NotificationCategory> registeredCategories = new List<NotificationCategory>();
        public void RegisterCategory(NotificationCategory category) => this.registeredCategories.Add(category);


        public virtual NotificationCompat.Builder CreateNativeBuilder(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.settings.IncrementValue("NotificationId");

            var pendingIntent = this.GetLaunchPendingIntent(notification);
            var builder = new NotificationCompat.Builder(this.context.AppContext)
                .SetContentTitle(notification.Title)
                .SetSmallIcon(this.GetSmallIconResource(notification))
                .SetAutoCancel(notification.Android.AutoCancel)
                .SetOngoing(notification.Android.OnGoing)
                .SetContentIntent(pendingIntent);

            if (!notification.Android.ContentInfo.IsEmpty())
                builder.SetContentInfo(notification.Android.ContentInfo);

            if (!notification.Android.Ticker.IsEmpty())
                builder.SetTicker(notification.Android.Ticker);

            if (!notification.Android.Category.IsEmpty())
                builder.SetCategory(notification.Category);

            if (notification.Android.UseBigTextStyle)
                builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(notification.Message));
            else
                builder.SetContentText(notification.Message);

            this.TrySetSound(notification, builder);
            this.TrySetLargeIconResource(notification, builder);

            if (!notification.Category.IsEmpty())
                this.AddCategory(builder, notification);

            if (notification.BadgeCount != null)
                builder.SetNumber(notification.BadgeCount.Value);

            // disabled until System.Drawing reliable works in Xamarin again
            //if (notification.Android.Color != null)
            //    builder.SetColor(notification.Android.Color.Value)

            if (!notification.Android.ColorResourceName.IsEmpty())
            {
                if (this.context.IsMinApiLevel(21))
                {
                    var color = this.GetColor(notification.Android.ColorResourceName);
                    builder.SetColor(color);
                }
                else
                {
                    Log.Write(NotificationLogCategory.Notifications, "ColorResourceName is only supported on API 21+");
                }
            }

            if (notification.Android.Priority != null)
            {
                builder.SetPriority(notification.Android.Priority.Value);
                builder.SetDefaults(NotificationCompat.DefaultAll);
            }

            if (notification.Android.ShowWhen != null)
                builder.SetShowWhen(notification.Android.ShowWhen.Value);

            if (notification.Android.When != null)
                builder.SetWhen(notification.Android.When.Value.ToEpochMillis());

            if (notification.Android.Vibrate)
                builder.SetVibrate(new long[] { 500, 500 });

            if (this.context.IsMinApiLevel(26))
            {
                this.CreateChannel(notification);
                builder.SetChannelId(notification.Android.ChannelId);
            }

            return builder;
        }


        public virtual void SendNative(int id, Android.App.Notification notification)
            => this.manager.Notify(id, notification);


        public virtual Android.App.Notification CreateNativeNotification(Notification notification)
            => this.CreateNativeBuilder(notification).Build();


        protected virtual void CreateChannel(Notification notification)
        {
            if (!this.context.IsMinApiLevel(26))
                return;

            var channelId = notification.Android.ChannelId;
            var channel = this.manager.GetNotificationChannel(channelId);

            if (channel == null)
            {
                channel = new NotificationChannel(
                    channelId,
                    notification.Android.Channel,
                    notification.Android.NotificationImportance.ToNative()
                );
                var d = notification.Android.ChannelDescription;
                if (!d.IsEmpty())
                    channel.Description = d;

                this.manager.CreateNotificationChannel(channel);
            }

            if (this.context.IsMinApiLevel(26) && (notification.Sound?.IsCustomSound() ?? false))
            {
                var attributes = new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.NotificationRingtone)
                    .Build();

                var uri = Android.Net.Uri.Parse(notification.Sound.CustomPath);
                channel.SetSound(uri, attributes);
                channel.EnableVibration(notification.Android.Vibrate);
                this.manager.CreateNotificationChannel(channel);
            }
        }

        protected virtual PendingIntent GetLaunchPendingIntent(Notification notification, string? actionId = null)
        {
            Intent launchIntent;
            if (notification.Android?.LaunchActivityType == null)
            {
                launchIntent = this.context
                    .AppContext
                    .PackageManager
                    .GetLaunchIntentForPackage(this.context.Package.PackageName)
                    .SetFlags(notification.Android.LaunchActivityFlags.ToNative());
            }
            else
            {
                launchIntent = new Intent(this.context.AppContext, notification.Android.LaunchActivityType);
            }

            var notificationString = this.serializer.Serialize(notification);
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
                    .Create(this.context.AppContext)
                    .AddNextIntent(launchIntent)
                    .GetPendingIntent(notification.Id, (int)PendingIntentFlags.OneShot);
            }
            else
            {
                pendingIntent = PendingIntent.GetActivity(
                    this.context.AppContext,
                    notification.Id,
                    launchIntent,
                    PendingIntentFlags.OneShot
                );
            }
            return pendingIntent;
        }


        protected virtual int GetColor(string colorResourceName)
        {
            var colorResourceId = this.context.GetColorByName(colorResourceName);
            if (colorResourceId <= 0)
                throw new ArgumentException($"Color ResourceId for {colorResourceName} not found");

            return ContextCompat.GetColor(this.context.AppContext, colorResourceId);
        }


        protected virtual int GetSmallIconResource(Notification notification)
        {
            if (notification.Android.SmallIconResourceName.IsEmpty())
            {
                var id = this.context.GetResourceIdByName("notification");
                if (id > 0)
                    return id;

                return this.context.AppContext.ApplicationInfo.Icon;
            }
            var smallIconResourceId = this.context.GetResourceIdByName(notification.Android.SmallIconResourceName);
            if (smallIconResourceId <= 0)
                throw new ArgumentException($"Icon ResourceId for {notification.Android.SmallIconResourceName} not found");

            return smallIconResourceId;
        }


        protected void TrySetLargeIconResource(Notification notification, NotificationCompat.Builder builder)
        {
            if (notification.Android.LargeIconResourceName.IsEmpty())
                return;

            var iconId = this.context.GetResourceIdByName(notification.Android.LargeIconResourceName);
            if (iconId > 0)
                builder.SetLargeIcon(BitmapFactory.DecodeResource(this.context.AppContext.Resources, iconId));
        }


        protected virtual void TrySetSound(Notification notification, NotificationCompat.Builder builder)
        {
            if (notification.Sound == null)
                return;

            switch (notification.Sound.Type)
            {
                case NotificationSoundType.Priority:
                    builder.SetSound(Android.Provider.Settings.System.DefaultAlarmAlertUri);
                    break;

                case NotificationSoundType.Default:
                    builder.SetSound(Android.Provider.Settings.System.DefaultNotificationUri);
                    break;

                case NotificationSoundType.Custom:
                    var uri = Android.Net.Uri.Parse(notification.Sound.CustomPath);
                    builder.SetSound(uri);
                    break;

                case NotificationSoundType.None:
                    break;
            }
        }


        //https://segunfamisa.com/posts/notifications-direct-reply-android-nougat
        protected virtual void AddCategory(NotificationCompat.Builder builder, Notification notification)
        {
            if (notification.Category.IsEmpty() || !this.context.IsMinApiLevel(24))
                return;

            var category = this.registeredCategories.FirstOrDefault(x => x.Identifier.Equals(notification.Category));
            if (category == null)
            {
                Log.Write(NotificationLogCategory.Notifications, "No notification category found for " + notification.Category);
            }
            else
            {
                var notificationString = this.serializer.Serialize(notification);

                foreach (var action in category.Actions)
                {
                    switch (action.ActionType)
                    {
                        case NotificationActionType.OpenApp:
                            break;

                        case NotificationActionType.TextReply:
                            var textReplyAction = this.CreateTextReply(notification, action);
                            builder.AddAction(textReplyAction);
                            break;

                        case NotificationActionType.None:
                        case NotificationActionType.Destructive:
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
        protected virtual PendingIntent CreateActionIntent(Notification notification, NotificationAction action)
        {
            var intent = this.context.CreateIntent<NotificationBroadcastReceiver>(NotificationBroadcastReceiver.IntentAction);
            var content = this.serializer.Serialize(notification);
            intent
                .PutExtra("Notification", content)
                .PutExtra("Action", action.Identifier);

            counter++;
            var pendingIntent = PendingIntent.GetBroadcast(
                this.context.AppContext,
                counter,
                intent,
                PendingIntentFlags.UpdateCurrent
            );
            return pendingIntent;
        }


        protected virtual NotificationCompat.Action CreateAction(Notification notification, NotificationAction action)
        {
            var pendingIntent = this.CreateActionIntent(notification, action);
            var iconId = this.context.GetResourceIdByName(action.Identifier);
            var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent).Build();

            return nativeAction;
        }


        protected virtual NotificationCompat.Action CreateTextReply(Notification notification, NotificationAction action)
        {
            var pendingIntent = this.CreateActionIntent(notification, action);
            var input = new AndroidX.Core.App.RemoteInput.Builder("Result")
                .SetLabel(action.Title)
                .Build();

            var iconId = this.context.GetResourceIdByName(action.Identifier);
            var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent)
                .SetAllowGeneratedReplies(true)
                .AddRemoteInput(input)
                .Build();

            return nativeAction;
        }
    }
}
