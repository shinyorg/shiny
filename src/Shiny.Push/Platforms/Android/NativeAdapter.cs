using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Extensions;
using Android.Runtime;
using Firebase.Messaging;
using Shiny.Infrastructure;
using Shiny.Notifications;
using Shiny.Push.Infrastructure;


namespace Shiny.Push
{
    public class NativeAdapter : INativeAdapter
    {
        readonly INotificationManager notifications;
        readonly ISerializer serializer;


        public NativeAdapter(INotificationManager notifications,
                             ISerializer serializer,
                             IAndroidContext context)
        {
            this.notifications = notifications;

            // on activity
            context
                .WhenIntentReceived()
                .SubscribeAsync(intent => this.TryProcessIntent(intent));

            // broadcast
            ShinyPushNotificationBroadcastReceiver.ProcessIntent = intent => this.TryProcessIntent(intent);
            ShinyFirebaseService.NewToken = token => this.OnTokenRefreshed?.Invoke(token);
            ShinyFirebaseService.MessageReceived = async msg =>
            {
                if (this.OnReceived != null)
                {
                    var pr = this.FromNative(msg);
                    await this.OnReceived.Invoke(pr).ConfigureAwait(false);
                }
            };
        }


        public async Task TryProcessIntent(Intent intent)
        {
            //if (intent.HasExtra(IntentNotificationKey))
            var pr = this.FromIntent(intent);
            if (pr != null && this.OnEntry != null)
                await this.OnEntry.Invoke(pr.Value).ConfigureAwait(false);
        }


        public Func<PushNotification, Task>? OnReceived { get; set; }
        public Func<PushNotificationResponse, Task>? OnEntry { get; set; }
        public Func<string, Task>? OnTokenRefreshed { get; set; }


        public async Task<PushAccessState> RequestAccess()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = true;
            var task = await FirebaseMessaging.Instance.GetToken();
            var token = task.JavaCast<Java.Lang.String>().ToString();
            return new PushAccessState(AccessState.Available, token);
        }


        public async Task UnRegister()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = false;
            await Task.Run(() => FirebaseMessaging.Instance.DeleteToken()).ConfigureAwait(false);
        }


        PushNotificationResponse? FromIntent(Intent intent)
        {
            //var notificationString = intent.GetStringExtra(IntentNotificationKey);
            //var notification = this.serializer.Deserialize<Shiny.Notifications.Notification>(notificationString);

            //var action = intent.GetStringExtra(IntentActionKey);
            //var text = RemoteInput.GetResultsFromIntent(intent)?.GetString("Result");
            //var response = new PushNotificationResponse(notification, action, text);

            //await this.delegates.RunDelegates(x => x.OnEntry(response));
            return null;
        }

        PushNotification FromNative(RemoteMessage message)
        {
            Notification? notification = null;
            var native = message.GetNotification();

            if (native != null)
            {
                notification = new Notification
                {
                    Title = native.Title,
                    Message = native.Body,
                    Channel = native.ChannelId
                };
                if (!native.Icon.IsEmpty())
                    notification.Android.SmallIconResourceName = native.Icon;

                if (!native.Color.IsEmpty())
                    notification.Android.ColorResourceName = native.Color;
            }
            return new PushNotification(message.Data, notification);
        }
    }
}
