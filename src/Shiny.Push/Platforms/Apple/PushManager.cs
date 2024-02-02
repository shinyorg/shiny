using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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


    public void Start()
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

        this.RequestAccess()
            .ContinueWith(x =>
            {
                if (x.Exception != null)
                {
                    this.logger.LogWarning(x.Exception, "Failed to auto start push");
                }
                else if (x.Result.Status != AccessState.Available)
                {
                    this.logger.LogInformation("User has removed push notification access - " + x.Result.Status);
                }
                else
                {
                    this.logger.LogInformation("PushManager still has user permissions");
                }
            });
    }


    public async Task<PushAccessState> RequestAccess(UNAuthorizationOptions options, CancellationToken cancelToken = default)
    {
        if (AppleExtensions.IsSimulator)
            return new PushAccessState(AccessState.NotSupported, null);

        var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(options);
        if (!result.Item1)
            return PushAccessState.Denied; // or just restricted?

        var deviceToken = await this.RequestRawToken(cancelToken).ConfigureAwait(false);
        var nativeToken = deviceToken.ToPushTokenString();
        var regToken = nativeToken;

        if (this.provider != null)
            regToken = await this.provider.Register(deviceToken);

        // if original regtoken was not null (we had registered) and new reg token (provider or native) doesn't
        // match, we fire the token refresh delegate method
        if (this.RegistrationToken != null && this.RegistrationToken != regToken)
        {
            // TODO: do we want to fire this here, the user may call this and store from the result anyhow
            await this.services
                .RunDelegates<IPushDelegate>(
                    x => x.OnTokenRefreshed(regToken),
                    this.logger
                )
                .ConfigureAwait(false);
        }

        this.NativeToken = nativeToken;
        this.RegistrationToken = regToken;

        return new PushAccessState(AccessState.Available, this.RegistrationToken);
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


    // This is called when notification is received in the app.
    public void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        this.TryProcessIncomingNotification(
            notification,
            "Foreground remote notification received",
            notification =>
            {
                var options = this.services
                    .GetServices<IPushDelegate>()
                    .Select(x =>
                    {
                        try
                        {
                            return (x as IApplePushDelegate)?.GetPresentationOptions(notification);
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError(ex, $"Error executing ApplePushDelegate {x.GetType().FullName}.GetPresentationOptions");
                            return null;
                        }
                    })
                    .FirstOrDefault(x => x != null);

                this.platform.InvokeOnMainThread(() =>
                    completionHandler.Invoke(
                        options ?? UNNotificationPresentationOptions.List | UNNotificationPresentationOptions.Banner
                    )
                );
            }
        );
    }


    // This is called when we tap on notification alert
    public void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler)
    {
        // if this errors, high level event hub will catch
        if (response?.Notification?.Request?.Trigger is not UNPushNotificationTrigger push)
            return;

        this.logger.LogDebug("OnDidReceiveNotificationResponse - Background remote notification entry detected");
        var data = this.ToPushNotification(response.Notification);
        this.services
            .RunDelegates<IPushDelegate>(
                x => x.OnEntry(data),
                this.logger
            )
            .ContinueWith(_ =>
            {
                // This needs be invoked on MainThread,
                // otherwise iOS app crashes if we tap on push notification alert
                // from notification center, while App in Active state.
                this.platform.InvokeOnMainThread(() => completionHandler.Invoke());
            });
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
            .ContinueWith(x =>
            {
                var fetchResult = this.services
                    .GetServices<IPushDelegate>()
                    .Select(x =>
                    {
                        try
                        {
                            return (x as IApplePushDelegate)?.GetFetchResult(data);
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError(ex, $"Error executing ApplePushDelegate {x.GetType().FullName}.GetFetchResult");
                            return null;
                        }
                    })
                    .FirstOrDefault(x => x != null);

                this.platform.InvokeOnMainThread(
                    () => completionHandler.Invoke(fetchResult ?? UIBackgroundFetchResult.NewData)
                );
            });
    }


    // This is called when app is fresh launched.
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


    protected virtual void TryProcessIncomingNotification(UNNotification? notification, string logMessage, Action<PushNotification> completionHandler)
    {
        // if this errors, high level event hub will catch
        if (notification?.Request?.Trigger is not UNPushNotificationTrigger push)
            return;

        this.logger.LogDebug(logMessage);

        var data = this.ToPushNotification(notification);
        this.services
            .RunDelegates<IPushDelegate>(
                x => x.OnReceived(data),
                this.logger
            )
            .ContinueWith(_ => completionHandler.Invoke(data));
    }


    protected virtual PushNotification ToPushNotification(UNNotification notification)
    {
        var c = notification.Request.Content;
        var shinyNotification = new Notification(
            c.Title,
            c.Body
        );

        var dict = c.UserInfo?.FromNsDictionary() ?? new Dictionary<string, string>(0);
        var data = new PushNotification(dict, shinyNotification);

        return data;
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