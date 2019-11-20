using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Logging;
using Shiny.Settings;
using TaskStackBuilder = Android.App.TaskStackBuilder;
using Native = Android.App.NotificationManager;
using RemoteInput = Android.Support.V4.App.RemoteInput;


namespace Shiny.Notifications
{
    public class NotificationManager : INotificationManager
    {
        readonly AndroidContext context;
        readonly IServiceProvider services;
        readonly IRepository repository;
        readonly ISettings settings;
        readonly ISerializer serializer;
        readonly IJobManager jobs;

        NotificationManagerCompat? compatManager;
        Native? newManager;


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

            // auto process intent?
            //this.context
            //    .WhenActivityStatusChanged()
            //    .Where(x => x.Status == ActivityState.Created)
            //    .Subscribe(x => TryProcessIntent(x.Activity.Intent));

            if ((int) Build.VERSION.SdkInt >= 26)
            {
                this.newManager = Native.FromContext(context.AppContext);
            }
            else
            {
                this.compatManager = NotificationManagerCompat.From(context.AppContext);
            }
        }


        public static void TryProcessIntent(Intent intent) => ShinyHost
            .Resolve<AndroidNotificationProcessor>()
            .TryProcessIntent(intent);


        public Task Cancel(int id)
            => this.repository.Remove<Notification>(id.ToString());


        public async Task Clear()
        {
            this.newManager?.CancelAll();
            this.compatManager?.CancelAll();
            await this.repository.Clear<Notification>();
        }


        public async Task<IEnumerable<Notification>> GetPending()
            => await this.repository.GetAll<Notification>();


        public async Task<AccessState> RequestAccess()
        {
            if (!this.compatManager?.AreNotificationsEnabled() ?? false)
                return AccessState.Disabled;

            if (!this.newManager?.AreNotificationsEnabled() ?? false)
                return AccessState.Disabled;

            return await this.jobs.RequestAccess();
        }


        //https://stackoverflow.com/questions/45462666/notificationcompat-builder-deprecated-in-android-o
        public async Task Send(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.settings.IncrementValue("NotificationId");

            if (notification.ScheduleDate != null)
            {
                await this.repository.Set(notification.Id.ToString(), notification);
                return;
            }

            var iconId = this.GetIconResource(notification);
            var builder = new NotificationCompat.Builder(this.context.AppContext)
                .SetContentTitle(notification.Title)
                .SetContentText(notification.Message)
                .SetSmallIcon(iconId)
                .SetAutoCancel(notification.Android.AutoCancel)
                .SetOngoing(notification.Android.OnGoing);

            if (!notification.Category.IsEmpty())
                this.AddCategory(builder, notification);
            else
            {
                var pendingIntent = this.BuildPendingIntent(notification);
                builder.SetContentIntent(pendingIntent);
            }

            this.AddSound(builder);

            if (notification.BadgeCount != null)
                builder.SetNumber(notification.BadgeCount.Value);

            //if ((int)Build.VERSION.SdkInt >= 21 && notification.Android.Color != null)
            //    builder.SetColor(notification.Android.Color.Value)

            if (notification.Android.Priority != null)
                builder.SetPriority(notification.Android.Priority.Value);

            if (notification.Android.Vibrate)
                builder.SetVibrate(new long[] {500, 500});

            this.DoNotify(builder, notification);
            await this.services.SafeResolveAndExecute<INotificationDelegate>(x => x.OnReceived(notification));
        }


        public int Badge { get; set; }

        readonly List<NotificationCategory> registeredCategories = new List<NotificationCategory>();
        public void RegisterCategory(NotificationCategory category) => this.registeredCategories.Add(category);


        protected virtual void DoNotify(NotificationCompat.Builder builder, Notification notification)
        {
            if (this.newManager != null)
            {
                var channelId = notification.Android.ChannelId;

                if (this.newManager.GetNotificationChannel(channelId) == null)
                {
                    var channel = new NotificationChannel(
                        channelId,
                        notification.Android.Channel,
                        notification.Android.NotificationImportance.ToNative()
                    );
                    var d = notification.Android.ChannelDescription;
                    if (!d.IsEmpty())
                        channel.Description = d;

                    this.newManager.CreateNotificationChannel(channel);
                }

                builder.SetChannelId(channelId);
                this.newManager.Notify(notification.Id, builder.Build());
            }
            else if (this.compatManager != null)
            {
                this.compatManager.Notify(notification.Id, builder.Build());
            }
        }


        protected virtual PendingIntent BuildPendingIntent(Notification notification)
        {
            var launchIntent = this
                .context
                .AppContext
                .PackageManager
                .GetLaunchIntentForPackage(this.context.Package.PackageName)
                .SetFlags(notification.Android.LaunchActivityFlags.ToNative());

            var notificationString = this.serializer.Serialize(notification);
            launchIntent.PutExtra(AndroidNotificationProcessor.NOTIFICATION_KEY, notificationString);
            if (!notification.Payload.IsEmpty())
                launchIntent.PutExtra("Payload", notification.Payload);

            PendingIntent pendingIntent;
            if ((notification.Android.LaunchActivityFlags & AndroidActivityFlags.ClearTask) != 0)
            {
                pendingIntent = TaskStackBuilder
                    .Create(this.context.AppContext)
                    .AddNextIntent(launchIntent)
                    .GetPendingIntent(notification.Id, PendingIntentFlags.OneShot);
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


        protected virtual int GetIconResource(Notification notification)
        {
            if (notification.Android.SmallIconResourceName.IsEmpty())
                return this.context.AppContext.ApplicationInfo.Icon;

            var smallIconResourceId = this.context.GetResourceIdByName(notification.Android.SmallIconResourceName);
            if (smallIconResourceId <= 0)
                throw new ArgumentException($"Icon ResourceId for {notification.Android.SmallIconResourceName} not found");

            return smallIconResourceId;
        }


        protected virtual void AddSound(NotificationCompat.Builder builder)
        {
            if (Notification.CustomSoundFilePath.IsEmpty())
            {
                builder.SetSound(Android.Provider.Settings.System.DefaultNotificationUri);
            }
            else
            {
                var uri = Android.Net.Uri.Parse(Notification.CustomSoundFilePath);
                builder.SetSound(uri);
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
                Log.Write("Notifications", "No notification category found for " + notification.Category);
            }
            else
            {
                foreach (var action in category.Actions)
                {
                    switch (action.ActionType)
                    {
                        case NotificationActionType.None:
                            break;

                        case NotificationActionType.OpenApp:
                            break;

                        case NotificationActionType.TextReply:
                            var nativeAction = this.CreateTextReply(notification.Id, action);
                            builder.AddAction(nativeAction);
                            break;

                        case NotificationActionType.Destructive:
                            break;

                        default:
                            throw new ArgumentException("Invalid action type");
                    }
                }
            }
        }


        protected virtual NotificationCompat.Action CreateTextReply(int notificationId, NotificationAction action)
        {
            var input = new RemoteInput.Builder("Result")
                .SetLabel(action.Title)
                .Build();

            var intent = this.context.CreateIntent<NotificationBroadcastReceiver>(NotificationBroadcastReceiver.IntentAction);
            intent.PutExtra("NotificationId", notificationId);

            var pendingIntent = PendingIntent.GetBroadcast(this.context.AppContext, 100, intent, PendingIntentFlags.UpdateCurrent);

            //this.context.GetResourceIdByName(action.Identifier)
            var nativeAction = new NotificationCompat.Action.Builder(0, action.Title, pendingIntent)
                .SetAllowGeneratedReplies(true)
                .AddRemoteInput(input)
                .Build();

            return nativeAction;
        }
    }
}
