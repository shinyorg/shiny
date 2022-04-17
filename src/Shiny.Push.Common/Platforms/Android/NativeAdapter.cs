using System;
using System.Collections.Generic;
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
            config?.AssertValid();

            this.context = context;
            this.config = config;
        }


        public async Task TryProcessIntent(Intent intent)
        {
            // if activity equals click_action? or if intent has a notificationId?
            if (intent != null && intent.HasExtra("google.message_id"))
            {
                var dict = new Dictionary<string, string>();
                foreach (var key in intent.Extras!.KeySet()!)
                {
                    var value = intent.Extras.Get(key)?.ToString();
                    if (value != null)
                        dict.Add(key, value);
                }
                var push = new PushNotification(dict);
                if (this.onEntry != null)
                    await this.onEntry.Invoke(push).ConfigureAwait(false);
            }
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
                        //Notification? notification = null;
                        //var native = msg.GetNotification();
                        //if (native != null)
                        //{
                        //}
                        // TODO: I have to send the notification here if in the foreground
                        //if (native != null)
                        //{
                        //    notification = new Notification
                        //    {
                        //        Title = native.Title,
                        //        Message = native.Body,
                        //        Channel = native.ChannelId
                        //    };
                        //    if (!native.Icon.IsEmpty())
                        //        notification.Android.SmallIconResourceName = native.Icon;

                            //    if (!native.Color.IsEmpty())
                            //        notification.Android.ColorResourceName = native.Color;
                        //}
                        var push = new PushNotification(msg.Data);
                        await this.onReceived.Invoke(push).ConfigureAwait(false);
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
        Func<PushNotification, Task>? onEntry;
        public Func<PushNotification, Task>? OnEntry
        {
            get => this.onEntry;
            set
            {
                this.onEntry = value;
                if (this.onEntry == null)
                {
                    this.onEntrySub?.Dispose();
                }
                else
                {
                    this.onEntrySub = this.context
                        .WhenIntentReceived()
                        .SubscribeAsync(intent => this.TryProcessIntent(intent));
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
                    ShinyFirebaseService.NewToken = token => this.onToken.Invoke(token);
                }
            }
        }


        bool initialized = false;
        public async Task<PushAccessState> RequestAccess()
        {
            if (!this.initialized)
            { 
                if (this.config == null)
                {
                    FirebaseApp.InitializeApp(this.context.AppContext);
                    if (FirebaseApp.Instance == null)
                        throw new InvalidOperationException("Firebase did not initialize.  Ensure your google.services.json is property setup.  Install the nuget package `Xamarin.GooglePlayServices.Tasks` into your Android head project, restart visual studio, and then set your google-services.json to GoogleServicesJson");
                }
                else
                {
                    var options = new FirebaseOptions.Builder()
                        .SetApplicationId(this.config.AppId)
                        .SetProjectId(this.config.ProjectId)
                        .SetApiKey(this.config.ApiKey)
                        .SetGcmSenderId(this.config.SenderId)
                        .Build();

                    FirebaseApp.InitializeApp(this.context.AppContext, options);
                }
                this.initialized = true;
            }
            var task = await FirebaseMessaging.Instance.GetToken();
            var token = task.JavaCast<Java.Lang.String>().ToString();
            return new PushAccessState(AccessState.Available, token);
        }


        public Task UnRegister() => Task.Run(() => FirebaseMessaging.Instance.DeleteToken());
    }
}
