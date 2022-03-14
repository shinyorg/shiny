using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Extensions;
using Android.Runtime;
using Firebase;
using Firebase.Messaging;
using Shiny.Push.Infrastructure;


namespace Shiny.Push
{
    public class NativeAdapter : INativeAdapter
    {
        readonly IPlatform context;
        readonly FirebaseConfig? config;


        public NativeAdapter(IPlatform context, FirebaseConfig? config = null)
        {
            this.context = context;
            this.config = config;
        }


        public async Task TryProcessIntent(Intent intent)
        {
            //var pr = this.notifications.FromIntent(intent);
            //if (pr != null && this.OnEntry != null)
            //    await this.OnEntry.Invoke(pr.Value).ConfigureAwait(false);
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
                        //var pr = AndroidPushNotificationManager.FromNative(msg);
                        //await this.onReceived.Invoke(pr).ConfigureAwait(false);
                        // stop sending this for now
                        //if (pr.Notification != null)
                        //{
                        //    // TODO: channel
                            //var nn = this.notifications.CreateNativeNotification(pr.Notification, null);
                        //    this.notifications.SendNative(0, nn);
                        //}
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
                var options = new FirebaseOptions.Builder()
                    .SetApplicationId(this.config.AppId)
                    //.SetProjectId(this.config.ProjectId)
                    .SetApiKey(this.config.ApiKey)
                    .SetGcmSenderId(this.config.SenderId)
                    .Build();
                FirebaseApp.InitializeApp(this.context.AppContext, options);
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
    }
}
