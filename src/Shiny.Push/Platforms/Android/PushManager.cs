using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Android.Runtime;
using Firebase;
using Firebase.Messaging;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny.Push;


public class PushManager : NotifyPropertyChanged,
                           IPushManager,
                           IShinyStartupTask,
                           IAndroidLifecycle.IOnActivityNewIntent
{
    readonly AndroidPlatform platform;
    readonly IServiceProvider services;
    readonly FirebaseConfig config;
    readonly ILogger logger;
    readonly IPushProvider provider;


    public PushManager(
        AndroidPlatform platform,
        FirebaseConfig config,
        IServiceProvider services,
        ILogger<PushManager> logger,
        IPushProvider? provider = null
    )
    {
        this.platform = platform;
        this.config = config;
        this.services = services;
        this.logger = logger;
        this.provider = provider ?? new FirebasePushProvider();
    }


    public async void Start()
    {
        if (this.RegistrationToken.IsEmpty())
            return;

        try
        {
            this.NativeToken = await this.RequestNativeToken();
            var regToken = await this.provider.Register(this.NativeToken); // never null on firebase
            if (regToken != this.RegistrationToken)
            {
                this.RegistrationToken = regToken;
                await this.services
                    .RunDelegates<IPushDelegate>(x => x.OnTokenRefreshed(regToken))
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "There was an error restarting push services");
        }
    }


    public IPushTagSupport? Tags => (this.provider as IPushTagSupport);

    string? regToken;
    public string? RegistrationToken
    {
        get => this.regToken;
        set => this.Set(ref this.regToken, value);
    }


    string? nativeToken;
    public string? NativeToken
    {
        get => this.nativeToken;
        set => this.Set(ref this.nativeToken, value);
    }


    public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var access = await this.platform
                .RequestAccess(Manifest.Permission.PostNotifications)
                .ToTask(cancelToken);

            if (access != AccessState.Available)
                return PushAccessState.Denied;
        }
#endif
        this.NativeToken = await this.RequestNativeToken();
        this.RegistrationToken = await this.provider.Register(this.NativeToken); // never null on firebase

        return new PushAccessState(AccessState.Available, this.RegistrationToken);
    }


    public async Task UnRegister()
    {
        this.NativeToken = null;
        this.RegistrationToken = null;

        await this.provider.UnRegister().ConfigureAwait(false);
        await FirebaseMessaging.Instance.DeleteToken().AsAsync().ConfigureAwait(false);
    }


    public async void Handle(Activity activity, Intent intent)
    {
        var clickAction = intent?.Action?.Equals(ShinyIntents.NotificationClickAction, StringComparison.InvariantCultureIgnoreCase) ?? false;
        if (!clickAction)
            return;

        this.logger.LogDebug("Detected incoming remote notification intent");
        var dict = new Dictionary<string, string>();

        if (intent!.Extras != null)
        {
            foreach (var key in intent.Extras!.KeySet()!)
            {
                var value = intent.Extras.Get(key)?.ToString();
                if (value != null)
                    dict.Add(key, value);
            }
        }
        // TODO: can I extract the notification here?
        var data = new PushNotification(dict, null);
        await this.services
            .RunDelegates<IPushDelegate>(x => x.OnEntry(data))
            .ConfigureAwait(false);
    }

    
    async Task<string> RequestNativeToken()
    {
        this.DoInit();
        var task = await FirebaseMessaging.Instance.GetToken();
        var native = task.JavaCast<Java.Lang.String>().ToString();

        return native;
    }


    bool initialized = false;
    void DoInit()
    {
        if (this.initialized)
            return;

        if (this.config.UseEmbeddedConfiguration)
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

        ShinyFirebaseService.NewToken = async token =>
        {
            await this.services
                .RunDelegates<IPushDelegate>(x => x.OnTokenRefreshed(token))
                .ConfigureAwait(false);
        };

        ShinyFirebaseService.MessageReceived = async msg =>
        {
            try
            {
                Notification? notification = null;
                var native = msg.GetNotification();

                if (native != null)
                {
                    //native.ChannelId
                    //native.ImageUrl
                    notification = new Notification(
                        native.Title,
                        native.Body
                    );
                    //this.TryTriggerNotification(msg);
                }
                var push = new PushNotification(msg.Data, notification);
                await this.services
                    .RunDelegates<IPushDelegate>(x => x.OnReceived(push))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {

            }
        };

        this.initialized = true;
    }


    //    protected virtual void TryTriggerNotification(RemoteMessage message)
    //    {
    //        try
    //        {
    //            var notification = message.GetNotification();
    //            var intent = new Intent(notification.ClickAction ?? ShinyIntents.NotificationClickAction);

    //            if (message.Data != null)
    //            {
    //                foreach (var data in message.Data)
    //                    intent.PutExtra(data.Key, data.Value);
    //            }

    //            var pendingIntent = PendingIntent.GetActivity(this.platform.AppContext, 99, intent, PendingIntentFlags.Mutable);

    //            var builder = new NotificationCompat
    //                .Builder(
    //                    this.platform.AppContext,
    //                    notification.ChannelId ?? Channel.Default.Identifier
    //                )
    //                .SetAutoCancel(true)
    //                .SetSilent(false)
    //                .SetSmallIcon(this.platform.GetSmallIconResource(notification.Icon))
    //                .SetContentIntent(pendingIntent)
    //                .SetContentTitle(notification.Title);

    //            if (!notification.Ticker.IsEmpty())
    //                builder.SetTicker(notification.Ticker);

    //            if (!notification.Body.IsEmpty())
    //                builder.SetContentText(notification.Body);

    //            ///this.platform.TrySetImage(notification.ImageUrl, builder);

    //            if (!notification.Color.IsEmpty())
    //            {
    //                var color = this.platform.GetColorResourceId(notification.Color);
    //                builder.SetColor(color);
    //            }

    //            var notificationId = this.settings.IncrementValue("NotificationId");
    //            this.platform
    //                .GetSystemService<NotificationManager>(Context.NotificationService)
    //                .Notify(notificationId, builder.Build());
    //        }
    //        catch (Exception ex)
    //        {
    //            this.logger.LogError("Error processing foreground remote notification", ex);
    //        }
    //    }
}