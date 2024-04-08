using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Android.OS;
using Android.Runtime;
using Firebase;
using Firebase.Messaging;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny.Push;


public class PushManager : NotifyPropertyChanged,
                           IPushManager,
                           IShinyStartupTask,
                           IAndroidLifecycle.IOnActivityOnCreate,
                           IAndroidLifecycle.IOnActivityNewIntent
{
    readonly AndroidPlatform platform;
    readonly IServiceProvider services;
    readonly FirebaseConfig config;
    readonly ILogger logger;
    readonly IPushProvider provider;
    bool registrationRequest = false;

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
        this.TryCreateConfiguredChannel();
        if (this.RegistrationToken.IsEmpty())
            return;

        try
        {
            this.NativeRegistrationToken = await this.RequestNativeToken();
            var regToken = await this.provider.Register(this.NativeRegistrationToken); // never null on firebase

            if (regToken != this.RegistrationToken)
            {
                this.RegistrationToken = regToken;
                await this.services
                    .RunDelegates<IPushDelegate>(
                        x => x.OnNewToken(regToken),
                        this.logger
                    )
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "There was an error restarting push services");
        }
    }


    void TryCreateConfiguredChannel()
    {
        if (this.config.DefaultChannel == null)
            return;

        using var nativeManager = this.platform.GetSystemService<NotificationManager>(Context.NotificationService);
        var channel = nativeManager.GetNotificationChannel(this.config.DefaultChannel.Id);
        if (channel != null)
            nativeManager.DeleteNotificationChannel(channel.Id);

        nativeManager.CreateNotificationChannel(this.config.DefaultChannel);
    }


    public IPushTagSupport? Tags => (this.provider as IPushTagSupport);

    string? regToken;
    public string? RegistrationToken
    {
        get => this.regToken;
        set => this.Set(ref this.regToken, value);
    }


    string? nativeToken;
    public string? NativeRegistrationToken
    {
        get => this.nativeToken;
        set => this.Set(ref this.nativeToken, value);
    }


    public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
    {
        this.registrationRequest = true;
        try
        {
            // TODO: verify google signed in
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                var access = await this.platform
                    .RequestAccess(Manifest.Permission.PostNotifications)
                    .ToTask(cancelToken);

                if (access != AccessState.Available)
                    return PushAccessState.Denied;
            }

            var nativeToken = await this.RequestNativeToken();
            var regToken = await this.provider.Register(nativeToken); // never null on firebase

            if (regToken != null && this.RegistrationToken != regToken)
            {
                await this.services
                    .RunDelegates<IPushDelegate>(
                        x => x.OnNewToken(regToken),
                        this.logger
                    )
                    .ConfigureAwait(false);
            }
            this.NativeRegistrationToken = nativeToken;
            this.RegistrationToken = regToken;

            return new PushAccessState(AccessState.Available, this.RegistrationToken);
        }
        finally
        {
            this.registrationRequest = false;
        }
    }


    public async Task UnRegister()
    {
        if (this.RegistrationToken == null)
            return;

        await this.provider
            .UnRegister()
            .ConfigureAwait(false);

        await FirebaseMessaging
            .Instance
            .DeleteToken()
            .AsAsync()
            .ConfigureAwait(false);

        await this.services
            .RunDelegates<IPushDelegate>(
                x => x.OnUnRegistered(this.RegistrationToken!),
                this.logger
            )
            .ConfigureAwait(false);

        this.NativeRegistrationToken = null;
        this.RegistrationToken = null;
    }


    public void ActivityOnCreate(Activity activity, Bundle? savedInstanceState)
        => this.Handle(activity, activity.Intent);

    public void Handle(Activity activity, Intent? intent)
    {
        var intentAction = this.config.IntentAction ?? ShinyPushIntents.NotificationClickAction;
        var clickAction = intent?.Action?.Equals(intentAction, StringComparison.InvariantCultureIgnoreCase) ?? false;
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
        // can I extract the notification here?
        var data = new PushNotification(dict, null);
        this.services
            .RunDelegates<IPushDelegate>(
                x => x.OnEntry(data),
                this.logger
            )
            .ContinueWith(x =>
                this.logger.LogInformation("Finished executing push delegates")
            );
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

        if (!this.IsFirebaseAappAlreadyInitialized())
        {
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
        }

        ShinyFirebaseService.NewToken = async token =>
        {
            if (this.NativeRegistrationToken == token || this.registrationRequest)
                return;

            this.NativeRegistrationToken = token;
            this.RegistrationToken = await this.provider.Register(token);
            
            await this.services
                .RunDelegates<IPushDelegate>(
                    x => x.OnNewToken(this.RegistrationToken),
                    this.logger
                )
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
                    notification = new Notification(
                        native.Title,
                        native.Body
                    );
                }
                var push = new AndroidPushNotification(
                    notification,
                    msg,
                    this.config,
                    this.platform
                );
                await this.services
                    .RunDelegates<IPushDelegate>(
                        x => x.OnReceived(push),
                        this.logger
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to receive firebase message");
            }
        };

        this.initialized = true;
    }


    bool IsFirebaseAappAlreadyInitialized()
    {
        var isAppInitialized = false;
        var firebaseApps = FirebaseApp.GetApps(this.platform.AppContext);
        foreach (var app in firebaseApps)
        {
            if (string.Equals(app.Name, FirebaseApp.DefaultAppName))
            {
                isAppInitialized = true;
                break;
            }
        }

        return isAppInitialized;
    }
}