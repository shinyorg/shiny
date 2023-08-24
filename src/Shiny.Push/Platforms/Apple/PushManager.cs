using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using UIKit;
using UserNotifications;
namespace Shiny.Push;


public class PushManager : NotifyPropertyChanged,
                           IApplePushManager,
                           IIosLifecycle.IOnFinishedLaunching,
                           IIosLifecycle.IRemoteNotifications,
                           IIosLifecycle.INotificationHandler
{
    static readonly NSString apsKey = new NSString("aps");
    static readonly NSString alertKey = new NSString("alert");

    readonly IServiceProvider services;
    readonly IPlatform platform;
    readonly ILogger logger;
    readonly IPushProvider? provider;
    
    TaskCompletionSource<NSData>? tokenSource;


    public PushManager(
        IServiceProvider services,
        IPlatform platform,
        ILogger<PushManager> logger,
        IPushProvider? provider = null
    )
    {
        this.services = services;
        this.platform = platform;
        this.logger = logger;
        this.provider = provider;
    }


    public IPushTagSupport? Tags => (this.provider as IPushTagSupport);


    string? registrationToken;
    public string? RegistrationToken
    {
        get => this.registrationToken;
        set => this.Set(ref this.registrationToken, value);
    }


    string? nativeToken;
    public string? NativeToken
    {
        get => this.nativeToken;
        set => this.Set(ref this.nativeToken, value);
    }


    public async void Start()
    {
        //AppleExtensions.AssertAppDelegateHook(
        //    "application:didReceiveRemoteNotification:fetchCompletionHandler:",
        //    "[SHINY] AppDelegate.DidReceiveRemoteNotification is not hooked - background notifications will not work without this!"
        //);

        //// application:didRegisterForRemoteNotificationsWithDeviceToken:"
        //AppleExtensions.AssertAppDelegateHook(
        //    "application:didRegisterForRemoteNotificationsWithDeviceToken:",
        //    "[SHINY] AppDelegate.RegisteredForRemoteNotifications is not hooked. This is a necessary hook for Shiny Push"
        //);

        ////application: didFailToRegisterForRemoteNotificationsWithError
        //AppleExtensions.AssertAppDelegateHook(
        //    "application:didFailToRegisterForRemoteNotificationsWithError:",
        //    "[SHINY] AppDelegate.FailedToRegisterForRemoteNotifications is not hooked. This is a necessary hook for Shiny Push"
        //);
        if (this.RegistrationToken.IsEmpty())
            return;

        try
        {
            // TODO: native token may already have been saved
            var result = await this.RequestAccessInternal().ConfigureAwait(false);
            if (result.Status == AccessState.Available && result.RegistrationToken != this.RegistrationToken)
            {
                this.RegistrationToken = result.RegistrationToken;
                await this.services
                    .RunDelegates<IPushDelegate>(x =>
                        x.OnTokenRefreshed(result.RegistrationToken!),
                        this.logger
                    )
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning("Failed to auto start push", ex);
        }
    }


    public async Task<PushAccessState> RequestAccess(UNAuthorizationOptions options, CancellationToken cancelToken = default)
    {
        var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(options);
        if (!result.Item1)
            return PushAccessState.Denied; // or just restricted?

        var access = await this.RequestAccessInternal(cancelToken).ConfigureAwait(false);
        if (access.Status == AccessState.Available)
            this.RegistrationToken = access.RegistrationToken;
        
        return access;
    }


    public Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        => this.RequestAccess(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound, cancelToken);


    public async Task UnRegister()
    {
        await this.platform
            .InvokeOnMainThreadAsync(UIApplication.SharedApplication.UnregisterForRemoteNotifications)
            .ConfigureAwait(false);

        if (this.provider != null)
            await this.provider.UnRegister().ConfigureAwait(false);
    }


    protected async Task<PushAccessState> RequestAccessInternal(CancellationToken cancelToken = default)
    {
        var deviceToken = await this.RequestRawToken(cancelToken).ConfigureAwait(false);
        if (this.provider != null)
            await this.provider.Register(deviceToken);

        var token = deviceToken.ToPushTokenString();
        return new PushAccessState(AccessState.Available, token);
    }


    protected async Task<NSData> RequestRawToken(CancellationToken cancelToken)
    {
        this.tokenSource = new();
        using var cancelSrc = cancelToken.Register(() => this.tokenSource.TrySetCanceled());

        await this.platform
            .InvokeOnMainThreadAsync(
                () => UIApplication
                    .SharedApplication
                    .RegisterForRemoteNotifications()
            )
            .ConfigureAwait(false);

        var rawToken = await this.tokenSource.Task.ConfigureAwait(false);
        return rawToken;
    }


    public void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        this.TryProcessIncomingNotification(
            notification,
            "Foreground remote notification received",
            () => completionHandler.Invoke(UNNotificationPresentationOptions.List | UNNotificationPresentationOptions.Banner)
        );
    }


    public void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler)
    {
        this.TryProcessIncomingNotification(
            response?.Notification,
            "Background remote notification entry detected",
            completionHandler
        );
    }


    public void OnRegistered(NSData deviceToken) => this.tokenSource?.TrySetResult(deviceToken);
    public void OnFailedToRegister(NSError error) => this.tokenSource?.TrySetException(new Exception(error.LocalizedDescription));
    public void OnDidReceive(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    {
        this.logger.LogDebug("Incoming Background remote notification");

        var dict = userInfo.FromNsDictionary();
        var data = new PushNotification(dict, null);
        this.services
            .RunDelegates<IPushDelegate>(
                x => x.OnReceived(data),
                this.logger
            )
            .ContinueWith(_ => completionHandler.Invoke(UIBackgroundFetchResult.NewData));
    }


    public void Handle(UIApplicationLaunchEventArgs args)
    {
        if (args.RemoteNotifications == null)
            return;

        this.logger.LogDebug("App entry remote notification detected");
        var notification = this.ToNotification(args.RemoteNotifications);
        var dict = args.RemoteNotifications.FromNsDictionary();
        dict.Remove("aps");

        var push = new PushNotification(dict ?? new Dictionary<string, string>(0), notification);
        this.services.RunDelegates<IPushDelegate>(
            x => x.OnEntry(push),
            this.logger
        );
    }


    protected virtual void TryProcessIncomingNotification(UNNotification? notification, string logMessage, Action completionHandler)
    {
        // if this errors, high level event hub will catch
        if (notification?.Request?.Trigger is not UNPushNotificationTrigger push)
            return;

        this.logger.LogDebug(logMessage);

        var c = notification.Request.Content;
        var shinyNotification = new Notification(
            c.Title,
            c.Body
        );

        var dict = c.UserInfo?.FromNsDictionary() ?? new Dictionary<string, string>(0);
        var data = new PushNotification(dict, shinyNotification);

        this.services
            .RunDelegates<IPushDelegate>(
                x => x.OnReceived(data),
                this.logger
            )
            .ContinueWith(_ => completionHandler.Invoke());
    }


    protected virtual Notification? ToNotification(NSDictionary data)
    {
        if (data.ContainsKey(apsKey))
        {
            var apsDict = data[apsKey] as NSDictionary;

            if (apsDict?.ContainsKey(alertKey) ?? false)
            {
                var alertDict = apsDict[alertKey] as NSDictionary;
                // category
                if (alertDict != null)
                {
                    return new Notification(
                        alertDict["title"]?.ToString(),
                        alertDict["body"]?.ToString()
                    );
                }
            }
        }
        return null;
    }
}