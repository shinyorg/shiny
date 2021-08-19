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
        readonly AndroidPushNotificationManager notifications;
        readonly ISerializer serializer;
        readonly IAndroidContext context;


        public NativeAdapter(AndroidPushNotificationManager notifications,
                             ISerializer serializer,
                             IAndroidContext context)
        {
            this.notifications = notifications;
            this.serializer = serializer;
            this.context = context;
        }


        public async Task TryProcessIntent(Intent intent)
        {
            //if (intent.HasExtra(IntentNotificationKey))
            var pr = this.FromIntent(intent);
            if (pr != null && this.OnEntry != null)
                await this.OnEntry.Invoke(pr.Value).ConfigureAwait(false);
        }


        Func<PushNotification, Task>? onReceived;
        public Func<PushNotification, Task>? OnReceived
        {
            get => this.onReceived;
            set
            {
                this.onReceived = value;
                if (this.onReceived == null)
                {
                    ShinyFirebaseService.MessageReceived = null;
                }
                else
                {
                    ShinyFirebaseService.MessageReceived = async msg =>
                    {
                        var pr = this.FromNative(msg);
                        await this.onReceived.Invoke(pr).ConfigureAwait(false);
                    };
                }
            }
        }


        IDisposable? onEntrySub;
        Func<PushNotificationResponse, Task>? onEntry;
        public Func<PushNotificationResponse, Task>? OnEntry
        {
            get => this.onEntry;
            set
            {
                this.onEntry = value;
                if (this.onEntry == null)
                {
                    this.onEntrySub?.Dispose();
                    ShinyPushNotificationBroadcastReceiver.ProcessIntent = null;
                }
                else
                {
                    this.onEntrySub = this.context
                        .WhenIntentReceived()
                        .SubscribeAsync(intent => this.TryProcessIntent(intent));

                    ShinyPushNotificationBroadcastReceiver.ProcessIntent = intent => this.TryProcessIntent(intent);
                }
            }
        }


        Func<string, Task>? onToken;
        public Func<string, Task>? OnTokenRefreshed
        {
            get => this.onToken;
            set
            {
                this.onToken = value;
                if (this.onToken == null)
                {
                    ShinyFirebaseService.NewToken = null;
                }
                else
                {
                    ShinyFirebaseService.NewToken = async token => await this.onToken.Invoke(token).ConfigureAwait(false);
                }
            }
        }


        public async Task<PushAccessState> RequestAccess()
        {
            //var options = new FirebaseOptions.Builder()
            //    //.SetApplicationId("") // Required for Analytics
            //    .SetProjectId("") // Required for Firebase Installations
            //    .SetApiKey("GOOGLE API KEY") // Required for Auth
            //    .Build();
            //FirebaseApp.InitializeApp(this.context.AppContext, options, "APP NAME");\

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

                //var nn = this.notifications.CreateNativeNotification(notification, null);
                //this.notifications.SendNative(0, nn);
            }
            return new PushNotification(message.Data, notification);
        }
    }
}
