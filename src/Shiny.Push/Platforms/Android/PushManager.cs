using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Firebase;
using Firebase.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;
using Shiny.Hosting;

namespace Shiny.Push;


public class PushManager(
    AndroidPlatform platform,
    FirebaseConfig config,
    IServiceProvider services,
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore store,
    ILogger<PushManager> logger,
    IPushProvider? pushProvider = null
) : IPushManager, IShinyStartupTask, IAndroidLifecycle.IOnActivityOnCreate, IAndroidLifecycle.IOnActivityNewIntent
{
    readonly AndroidPlatform platform = platform;
    readonly FirebaseConfig config = config;
    readonly IServiceProvider services = services;
    readonly IKeyValueStore store = store;
    readonly ILogger logger = logger;
    readonly IPushProvider provider = pushProvider ?? new FirebasePushProvider(store);
    bool registrationRequest = false;

    static string Key(string prop) => $"Shiny.Push.PushManager.{prop}";

    string? regToken = store.Get<string>(Key(nameof(RegistrationToken)));
    string? nativeToken = store.Get<string>(Key(nameof(NativeRegistrationToken)));


    public async void Start()
    {
        this.TryCreateConfiguredChannel();
        if (this.RegistrationToken.IsEmpty())
            return;

        try
        {
            // the user turned notifications off (or revoked POST_NOTIFICATIONS on 13+) since the last session
            if (!this.AreNotificationsEnabled())
            {
                this.logger.LogInformation("User has removed push notification access - unregistering");
                await this.UnRegister().ConfigureAwait(false);
                return;
            }

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


    bool AreNotificationsEnabled()
    {
        using var compat = NotificationManagerCompat.From(this.platform.AppContext)!;
        return compat.AreNotificationsEnabled();
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

    public string? RegistrationToken
    {
        get => this.regToken;
        set
        {
            if (this.regToken != value)
            {
                this.regToken = value;
                this.store.SetOrRemove(Key(nameof(RegistrationToken)), value);
            }
        }
    }


    public string? NativeRegistrationToken
    {
        get => this.nativeToken;
        set
        {
            if (this.nativeToken != value)
            {
                this.nativeToken = value;
                this.store.SetOrRemove(Key(nameof(NativeRegistrationToken)), value);
            }
        }
    }


    public Task<AccessState> GetCurrentAccess()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = this.platform.GetCurrentPermissionStatus(Manifest.Permission.PostNotifications);
            return Task.FromResult(status);
        }
        return Task.FromResult(AccessState.Available);
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
                    .WaitAsync(cancelToken);

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

        // Start can unregister before anything in this process has touched Firebase
        this.DoInit();

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
    {
        // a recreated activity (rotation, process restore) or one reopened from recents still carries the intent
        // that launched it - processing it again would run OnEntry a second time for the same tap
        var fromHistory = activity.Intent?.Flags.HasFlag(ActivityFlags.LaunchedFromHistory) ?? false;
        if (savedInstanceState == null && !fromHistory)
            this.Handle(activity, activity.Intent);
    }

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

        // async void (Action<string>) - an exception escaping it would crash the app, so nothing may
        ShinyFirebaseService.NewToken = async token =>
        {
            try
            {
                // not registered (never, or unregistered - FCM can mint a new token after DeleteToken): only
                // RequestAccess registers, so a refresh must not quietly register the device again
                if (this.RegistrationToken == null || this.NativeRegistrationToken == token || this.registrationRequest)
                    return;

                // the provider (e.g. a hub installation) is registered before the token is stored, so a failure here
                // leaves the old native token in place and the next refresh or app start tries again
                var regToken = await this.provider.Register(token).ConfigureAwait(false);
                this.NativeRegistrationToken = token;
                this.RegistrationToken = regToken;

                await this.services
                    .RunDelegates<IPushDelegate>(
                        x => x.OnNewToken(regToken),
                        this.logger
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to register the refreshed push token");
            }
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
                        native.Body,
                        native.ImageUrl?.ToString() ?? PushImages.Find(msg.Data, this.config.ImageKeys)
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