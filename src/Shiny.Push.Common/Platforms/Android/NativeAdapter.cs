using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Android.Runtime;
using AndroidX.Core.App;
using Firebase;
using Firebase.Messaging;
using Microsoft.Extensions.Logging;
using Shiny.Notifications;
using Shiny.Push.Infrastructure;


namespace Shiny.Push
{
    public class NativeAdapter : INativeAdapter
    {
        readonly IPlatform platform;
        readonly FirebaseConfig? config;
        readonly ILogger logger;


        public NativeAdapter(IPlatform platform, ILogger<NativeAdapter> logger, FirebaseConfig? config = null)
        {
            config?.AssertValid();

            this.platform = platform;
            this.logger = logger;
            this.config = config;
        }


        public async Task TryProcessIntent(Intent intent)
        {
            // if activity equals click_action? or if intent has a notificationId?
            if (intent != null && intent.HasExtra("google.message_id"))
            {
                this.logger.LogDebug("Detected incoming remote notification intent");

                if (this.onEntry == null)
                {
                    this.logger.LogWarning("OnEntry is not hooked");
                }
                else
                {
                    try
                    {
                        var dict = new Dictionary<string, string>();
                        foreach (var key in intent.Extras!.KeySet()!)
                        {
                            var value = intent.Extras.Get(key)?.ToString();
                            if (value != null)
                                dict.Add(key, value);
                        }
                        var push = new PushNotification(dict);

                        await this.onEntry.Invoke(push).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError("Error processing onEntry", ex);
                    }
                }
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
                        this.TryTriggerNotification(msg);
                        var push = new PushNotification(msg.Data);
                        await this.onReceived.Invoke(push).ConfigureAwait(false);
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
                    this.onEntrySub = this.platform
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
                    FirebaseApp.InitializeApp(this.platform.AppContext);
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

                    FirebaseApp.InitializeApp(this.platform.AppContext, options);
                }
                this.initialized = true;
            }
            var task = await FirebaseMessaging.Instance.GetToken();
            var token = task.JavaCast<Java.Lang.String>().ToString();
            return new PushAccessState(AccessState.Available, token);
        }


        public Task UnRegister() => Task.Run(() => FirebaseMessaging.Instance.DeleteToken());


        protected virtual void TryTriggerNotification(RemoteMessage message)
        {
            try
            {
                var notification = message.GetNotification();
                if (notification == null)
                    return;

                var notificationId = Int32.Parse(message.MessageId);
                var builder = new NotificationCompat.Builder(this.platform.AppContext, notification.ChannelId)
                    .SetContentTitle(notification.Title);

                if (!notification.Icon.IsEmpty())
                    builder.SetSmallIcon(this.platform.GetSmallIconResource(notification.Icon));

                if (!notification.Ticker.IsEmpty())
                    builder.SetTicker(notification.Ticker);

                if (!notification.Body.IsEmpty())
                    builder.SetContentText(notification.Body);

                this.platform.TrySetImage(notification.ImageUrl, builder);

                if (!notification.Color.IsEmpty())
                {
                    var color = this.platform.GetColorResourceId(notification.Color);
                    builder.SetColor(color);
                }
                this.platform
                    .GetSystemService<NotificationManager>(Context.NotificationService)
                    .Notify(notificationId, builder.Build());
            }
            catch (Exception ex)
            {
                this.logger.LogError("Error processing foreground remote notification", ex);
            }
        }
    }
}
