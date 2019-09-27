using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Settings;
using TaskStackBuilder = Android.App.TaskStackBuilder;
using Native = Android.App.NotificationManager;


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

        Native newManager;
        NotificationManagerCompat compatManager;


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

            PendingIntent pendingIntent = null;
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

            var smallIconResourceId = this.context.GetResourceIdByName(notification.Android.SmallIconResourceName);
            if (smallIconResourceId <= 0)
                throw new ArgumentException($"No ResourceId found for '{notification.Android.SmallIconResourceName}' - You can set this per notification using notification.Android.SmallIconResourceName or globally using Shiny.Android.AndroidOptions.SmallIconResourceName");

            var builder = new NotificationCompat.Builder(this.context.AppContext)
                .SetContentTitle(notification.Title)
                .SetContentText(notification.Message)
                .SetSmallIcon(smallIconResourceId)
                .SetContentIntent(pendingIntent);

            if (notification.BadgeCount > 0)
                builder.SetNumber(notification.BadgeCount);

            //if ((int)Build.VERSION.SdkInt >= 21 && notification.Android.Color != null)
            //    builder.SetColor(notification.Android.Color.Value)

            builder.SetAutoCancel(notification.Android.AutoCancel);
            builder.SetOngoing(notification.Android.OnGoing);

            if (notification.Android.Priority != null)
                builder.SetPriority(notification.Android.Priority.Value);

            if (notification.Android.Vibrate)
                builder.SetVibrate(new long[] {500, 500});

            if (Notification.CustomSoundFilePath.IsEmpty())
            {
                builder.SetSound(Android.Provider.Settings.System.DefaultNotificationUri);
            }
            else
            {
                var uri = Android.Net.Uri.Parse(Notification.CustomSoundFilePath);
                builder.SetSound(uri);
            }

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
            else
            {
                this.compatManager.Notify(notification.Id, builder.Build());
            }

            await this.services.SafeResolveAndExecute<INotificationDelegate>(x => x.OnReceived(notification));
        }
    }
}
