using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Logging;
using Shiny.Settings;
using Native = Android.App.NotificationManager;
using RemoteInput = Android.Support.V4.App.RemoteInput;
using TaskStackBuilder = Android.App.TaskStackBuilder;


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

            if (this.context.IsMinApiLevel(26))
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


        public async Task Cancel(int id)
        {
            this.newManager?.Cancel(id);
            this.compatManager?.Cancel(id);
            await this.repository.Remove<Notification>(id.ToString());
        }


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

            this.AddSound(builder);

            if (!notification.Category.IsEmpty())
                this.AddCategory(builder, notification);
            else
            {
                var pendingIntent = this.GetLaunchPendingIntent(notification);
                builder.SetContentIntent(pendingIntent);
            }

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
                builder.SetPriority(notification.Android.Priority.Value);

            if (notification.Android.ShowWhen != null)
                builder.SetShowWhen(notification.Android.ShowWhen.Value);

            if (notification.Android.When != null)
                builder.SetWhen(notification.Android.When.Value.ToEpochMillis());

            if (notification.Android.Vibrate)
                builder.SetVibrate(new long[] { 500, 500 });

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


        protected virtual PendingIntent GetLaunchPendingIntent(Notification notification, string actionId = null)
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


        protected virtual int GetColor(string colorResourceName)
        {
            var colorResourceId = this.context.GetColorByName(colorResourceName);
            if (colorResourceId <= 0)
                throw new ArgumentException($"Color ResourceId for {colorResourceName} not found");

            return ContextCompat.GetColor(this.context.AppContext, colorResourceId);
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
            var input = new RemoteInput.Builder("Result")
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
