using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Hosting;
using Shiny.Extensions.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Push;


public class PushManager(
    IServiceProvider services,
    IPlatform platform,
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore store,
    ILogger<PushManager> logger,
    IPushProvider? provider = null
) :
    IApplePushManager,
    IIosLifecycle.IOnFinishedLaunching,
    IIosLifecycle.IRemoteNotifications
#if !TVOS
    ,
    IIosLifecycle.INotificationHandler
#endif
{
    static readonly NSString apsKey = new("aps");
    static readonly NSString alertKey = new("alert");
    readonly SemaphoreSlim semaphore = new(1, 1);
    TaskCompletionSource<NSData>? tokenSource;

    static string Key(string prop) => $"Shiny.Push.PushManager.{prop}";

    public IPushTagSupport? Tags => provider as IPushTagSupport;


    string? registrationToken = store.Get<string>(Key(nameof(RegistrationToken)));
    public string? RegistrationToken
    {
        get => this.registrationToken;
        set
        {
            if (this.registrationToken != value)
            {
                this.registrationToken = value;
                store.SetOrRemove(Key(nameof(RegistrationToken)), value);
            }
        }
    }


    string? nativeToken = store.Get<string>(Key(nameof(NativeRegistrationToken)));
    public string? NativeRegistrationToken
    {
        get => this.nativeToken;
        set
        {
            if (this.nativeToken != value)
            {
                this.nativeToken = value;
                store.SetOrRemove(Key(nameof(NativeRegistrationToken)), value);
            }
        }
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

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        this.RequestAccess(cts.Token)
            .ContinueWith(x =>
            {
                if (x.IsCanceled)
                {
                    logger.LogWarning("Push auto-start timed out - ensure AppDelegate push hooks are wired");
                }
                else if (x.Exception != null)
                {
                    logger.LogWarning(x.Exception, "Failed to auto start push");
                }
                else if (x.Result.Status != AccessState.Available)
                {
                    logger.LogInformation("User has removed push notification access - " + x.Result.Status);
                }
                else
                {
                    logger.LogInformation("PushManager still has user permissions");
                }
            });
    }


    public async Task<AccessState> GetCurrentAccess()
    {
        if (AppleExtensions.IsSimulator)
            return AccessState.NotSupported;

        var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
        return settings.AuthorizationStatus switch
        {
            UNAuthorizationStatus.Authorized => AccessState.Available,
            UNAuthorizationStatus.Denied => AccessState.Denied,
            UNAuthorizationStatus.Provisional => AccessState.Available,
            UNAuthorizationStatus.Ephemeral => AccessState.Available,
            _ => AccessState.Unknown
        };
    }


    public async Task<PushAccessState> RequestAccess(UNAuthorizationOptions options, CancellationToken cancelToken = default)
    {
        if (AppleExtensions.IsSimulator)
            return new PushAccessState(AccessState.NotSupported, null);

        await this.semaphore.WaitAsync(cancelToken).ConfigureAwait(false);
        try
        {
            var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(options);
            if (!result.Item1)
                return PushAccessState.Denied; // or just restricted?

            var deviceToken = await this.RequestRawToken(cancelToken).ConfigureAwait(false);
            var nativeToken = deviceToken.ToPushTokenString();
            var regToken = nativeToken;

            if (provider != null)
                regToken = await provider.Register(deviceToken);

            if (regToken != null && this.RegistrationToken != regToken)
            {
                await services
                    .RunDelegates<IPushDelegate>(
                        x => x.OnNewToken(regToken),
                        logger
                    )
                    .ConfigureAwait(false);
            }

            this.NativeRegistrationToken = nativeToken;
            this.RegistrationToken = regToken;

            return new PushAccessState(AccessState.Available, this.RegistrationToken);
        }
        finally
        {
            this.semaphore.Release();
        }
    }


    public Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
#if TVOS
        // tvOS notifications only ever update the app icon badge - asking for Alert or Sound gets
        // you nothing and the authorization prompt is the poorer for it
        => this.RequestAccess(UNAuthorizationOptions.Badge, cancelToken);
#else
        => this.RequestAccess(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound, cancelToken);
#endif


    public async Task UnRegister()
    {
        await platform
            .InvokeOnMainThreadAsync(UIApplication
                .SharedApplication
                .UnregisterForRemoteNotifications
            )
            .ConfigureAwait(false);

        if (provider != null)
            await provider.UnRegister().ConfigureAwait(false);

        await services
            .RunDelegates<IPushDelegate>(
                x => x.OnUnRegistered(this.RegistrationToken!),
                logger
            )
            .ConfigureAwait(false);

        this.NativeRegistrationToken = null;
        this.RegistrationToken = null;
    }


    protected async Task<NSData> RequestRawToken(CancellationToken cancelToken)
    {
        this.tokenSource = new();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));
        using var cancelSrc = timeoutCts.Token.Register(() => this.tokenSource.TrySetCanceled());

        await platform
            .InvokeOnMainThreadAsync(
                () => UIApplication
                    .SharedApplication
                    .RegisterForRemoteNotifications()
            )
            .ConfigureAwait(false);

        var rawToken = await this.tokenSource.Task.ConfigureAwait(false);
        return rawToken;
    }


#if !TVOS
    // Neither hook exists on tvOS: notifications there can only change the app icon badge, so
    // nothing is presented in the foreground and there is no UNNotificationResponse to tap through
    // This is called when notification is received in the app.
    public void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        this.TryProcessIncomingNotification(
            notification,
            "Foreground remote notification received",
            notification =>
            {
                var options = services
                    .GetServices<IPushDelegate>()
                    .Select(x =>
                    {
                        try
                        {
                            return (x as IApplePushDelegate)?.GetPresentationOptions(notification);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Error executing ApplePushDelegate {x.GetType().FullName}.GetPresentationOptions");
                            return null;
                        }
                    })
                    .FirstOrDefault(x => x != null);

                platform.InvokeOnMainThread(() =>
                    completionHandler.Invoke(
                        options ??
                        UNNotificationPresentationOptions.List |
                        UNNotificationPresentationOptions.Banner
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

        logger.LogDebug("OnDidReceiveNotificationResponse - Background remote notification entry detected");
        var data = this.ToPushNotification(response.Notification);
        services
            .RunDelegates<IPushDelegate>(
                x => x.OnEntry(data),
                logger
            )
            .ContinueWith(_ =>
            {
                // This needs be invoked on MainThread,
                // otherwise iOS app crashes if we tap on push notification alert
                // from notification center, while App in Active state.
                platform.InvokeOnMainThread(() => completionHandler.Invoke());
            });
    }
#endif


    public void OnRegistered(NSData deviceToken) => this.tokenSource?.TrySetResult(deviceToken);
    public void OnFailedToRegister(NSError error) => this.tokenSource?.TrySetException(new Exception(error.LocalizedDescription));
    public void OnDidReceive(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    {
        logger.LogDebug("Incoming Background remote notification");

        var dict = userInfo.FromNsDictionary();
        var data = new PushNotification(dict, null);
        services
            .RunDelegates<IPushDelegate>(
                x => x.OnReceived(data),
                logger
            )
            .ContinueWith(x =>
            {
                var fetchResult = services
                    .GetServices<IPushDelegate>()
                    .Select(x =>
                    {
                        try
                        {
                            return (x as IApplePushDelegate)?.GetFetchResult(data);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Error executing ApplePushDelegate {x.GetType().FullName}.GetFetchResult");
                            return null;
                        }
                    })
                    .FirstOrDefault(x => x != null);

                platform.InvokeOnMainThread(
                    () => completionHandler.Invoke(fetchResult ?? UIBackgroundFetchResult.NewData)
                );
            });
    }


    // This is called when app is fresh launched.
    public void Handle(UIApplicationLaunchEventArgs args)
    {
#if TVOS
        // tvOS does not surface the launching remote notification on UIApplicationLaunchEventArgs -
        // a silent push that wakes the app arrives through OnDidReceive instead
#else
        if (args.RemoteNotifications == null)
            return;

        logger.LogDebug("App entry remote notification detected");
        var notification = this.ToNotification(args.RemoteNotifications);
        var dict = args.RemoteNotifications.FromNsDictionary();
        dict.Remove("aps");

        var push = new PushNotification(dict ?? new Dictionary<string, string>(0), notification);
        services.RunDelegates<IPushDelegate>(
            x => x.OnEntry(push),
            logger
        );
#endif
    }


#if !TVOS
    // UNNotificationContent on tvOS has no Title, Body or UserInfo - the only thing a tvOS
    // notification carries is the badge count - so there is no payload here to project
    protected virtual void TryProcessIncomingNotification(UNNotification? notification, string logMessage, Action<PushNotification> completionHandler)
    {
        // if this errors, high level event hub will catch
        if (notification?.Request?.Trigger is not UNPushNotificationTrigger push)
            return;

        logger.LogDebug(logMessage);

        var data = this.ToPushNotification(notification);
        services
            .RunDelegates<IPushDelegate>(
                x => x.OnReceived(data),
                logger
            )
            .ContinueWith(_ => completionHandler.Invoke(data));
    }


    
    protected virtual ApplePushNotification ToPushNotification(UNNotification notification)
    {
        var c = notification.Request.Content;
        var shinyNotification = new Notification(
            c.Title,
            c.Body
        );
   
        var dict = c.UserInfo?.FromNsDictionary() ?? new Dictionary<string, string>(0);
        var data = new ApplePushNotification(dict, c.UserInfo, shinyNotification);
   
        return data;
    }
#endif


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