using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Android.Runtime;
using Firebase;
using Firebase.Messaging;
using Shiny.Infrastructure;
using Shiny.Push.Infrastructure;
using Notification = Shiny.Notifications.Notification;


namespace Shiny.Push
{
    public class NativeAdapter : INativeAdapter
    {
        readonly AndroidPushNotificationManager notifications;
        readonly ISerializer serializer;
        readonly IAndroidContext context;
        readonly FirebaseConfig? config;


        public NativeAdapter(AndroidPushNotificationManager notifications,
                             ISerializer serializer,
                             IAndroidContext context,
                             FirebaseConfig? config = null)
        {
            this.notifications = notifications;
            this.serializer = serializer;
            this.context = context;
            this.config = config;
        }


        public async Task TryProcessIntent(Intent intent)
        {
            // TODO: if (intent.HasExtra(IntentNotificationKey))
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
                        if (pr.Notification != null)
                        {
                            // TODO: channel
                            var nn = this.notifications.CreateNativeNotification(pr.Notification, null);
                            this.notifications.SendNative(0, nn);
                        }
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
            if (this.config == null)
            {
                FirebaseMessaging.Instance.AutoInitEnabled = true;
            }
            else
            {
                //new FirebaseOptions.Builder().SetGcmSenderId
                var options = new FirebaseOptions.Builder()
                    .SetApplicationId(this.config.AppId)
                    .SetProjectId(this.config.ProjectId)
                    .SetApiKey(this.config.ApiKey)
                    .Build();
                FirebaseApp.InitializeApp(this.context.AppContext, options, this.config.AppName);

            }

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
            var notificationString = intent.GetStringExtra("TODO");
            var notification = this.serializer.Deserialize<Shiny.Notifications.Notification>(notificationString);

            var action = intent.GetStringExtra("TODO");
            var text = RemoteInput.GetResultsFromIntent(intent)?.GetString("Result");
            var response = new PushNotificationResponse(notification, action, text);

            return response;
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
