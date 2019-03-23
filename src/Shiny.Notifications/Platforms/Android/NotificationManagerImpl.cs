using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using TaskStackBuilder = Android.App.TaskStackBuilder;


namespace Shiny.Notifications
{
    public class NotificationManagerImpl : INotificationManager
    {
        readonly IAndroidContext context;

        NotificationManager newManager;
        NotificationManagerCompat compatManager;


        public NotificationManagerImpl(IAndroidContext context)
        {
            this.context = context;
            if ((int) Build.VERSION.SdkInt >= 26)
            {
                this.newManager = NotificationManager.FromContext(context.AppContext);
            }
            else
            {
                this.compatManager = NotificationManagerCompat.From(context.AppContext);
            }
        }


        public Task Clear()
        {
            this.newManager?.CancelAll();
            this.compatManager?.CancelAll();
            return Task.CompletedTask;
        }

        public Task<AccessState> RequestAccess()
        {
            var state = AccessState.Available;
            if (!this.compatManager?.AreNotificationsEnabled() ?? false)
                state = AccessState.Disabled;

            else if (!this.newManager?.AreNotificationsEnabled() ?? false)
                state = AccessState.Disabled;

            //var result = await this.Jobs.RequestAccess();
            //return result;
            return Task.FromResult(state);
        }


        //https://stackoverflow.com/questions/45462666/notificationcompat-builder-deprecated-in-android-o
        public async Task Send(Notification notification)
        {
            //if (notification.NextTriggerDate != null)
            //{
            //    // TODO: notificationID?
            //    await this.Repository.Set(notification.Id.ToString(), notification);
            //    return;
            //}

            var launchIntent = Application
                .Context
                .PackageManager
                .GetLaunchIntentForPackage(this.context.Package.PackageName)
                .SetFlags(notification.Android.LaunchActivityFlags.ToNative());

            if (!notification.Payload.IsEmpty())
                launchIntent.PutExtra("Payload", notification.Payload);

            var pendingIntent = TaskStackBuilder
                .Create(this.context.AppContext)
                .AddNextIntent(launchIntent)
                .GetPendingIntent(notification.Id, PendingIntentFlags.CancelCurrent);
            //.GetPendingIntent(notification.Id, PendingIntentFlags.OneShot | PendingIntentFlags.CancelCurrent);

            var smallIconResourceId = this.context.GetResourceIdByName(notification.Android.SmallIconResourceName);

            var builder = new NotificationCompat.Builder(this.context.AppContext)
                .SetAutoCancel(true)
                .SetContentTitle(notification.Title)
                .SetContentText(notification.Message)
                .SetSmallIcon(smallIconResourceId)
                .SetContentIntent(pendingIntent);

            // TODO
            //if ((int)Build.VERSION.SdkInt >= 21 && notification.Android.Color != null)
            //    builder.SetColor(notification.Android.Color.Value)

            if (notification.Android.Priority != null)
                builder.SetPriority(notification.Android.Priority.Value);

            if (notification.Android.Vibrate)
                builder.SetVibrate(new long[] {500, 500});

            if (notification.Sound != null)
            {
                if (!notification.Sound.Contains("://"))
                    notification.Sound =
                        $"{ContentResolver.SchemeAndroidResource}://{this.context.Package.PackageName}/raw/{notification.Sound}";

                var uri = Android.Net.Uri.Parse(notification.Sound);
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
        }
    }
}
