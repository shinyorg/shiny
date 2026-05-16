using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using UserNotifications;
using Shiny.Hosting;
using Shiny.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Push;


public class PushManager(
    IServiceProvider services,
    IPlatform platform,
    IKeyValueStore store,
    ILogger<PushManager> logger,
    IPushProvider? provider = null
) :
    NotifyPropertyChanged,
    IApplePushManager,
    IMacLifecycle.IOnFinishedLaunching,
    IMacLifecycle.IRemoteNotifications,
    IMacLifecycle.INotificationHandler
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
            if (this.Set(ref this.registrationToken, value))
                store.SetOrRemove(Key(nameof(RegistrationToken)), value);
        }
    }


    string? nativeToken = store.Get<string>(Key(nameof(NativeRegistrationToken)));
    public string? NativeRegistrationToken
    {
        get => this.nativeToken;
        set
        {
            if (this.Set(ref this.nativeToken, value))
                store.SetOrRemove(Key(nameof(NativeRegistrationToken)), value);
        }
    }


    public void Start()
    {
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
        await this.semaphore.WaitAsync(cancelToken).ConfigureAwait(false);
        try
        {
            var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(options);
            if (!result.Item1)
                return PushAccessState.Denied;

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
        => this.RequestAccess(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound, cancelToken);


    public async Task UnRegister()
    {
        await platform
            .InvokeOnMainThreadAsync(NSApplication
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
                () => NSApplication
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


    public void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler)
    {
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
                platform.InvokeOnMainThread(() => completionHandler.Invoke());
            });
    }


    public void OnRegistered(NSData deviceToken) => this.tokenSource?.TrySetResult(deviceToken);
    public void OnFailedToRegister(NSError error) => this.tokenSource?.TrySetException(new Exception(error.LocalizedDescription));

    public void OnDidReceive(NSDictionary userInfo)
    {
        logger.LogDebug("Incoming Background remote notification");

        var dict = userInfo.FromNsDictionary();
        var data = new PushNotification(dict, null);
        services.RunDelegates<IPushDelegate>(
            x => x.OnReceived(data),
            logger
        );
    }


    public void Handle(NSDictionary? launchOptions)
    {
        if (launchOptions == null)
            return;

        var key = new NSString("NSApplicationLaunchUserNotificationKey");
        if (!launchOptions.ContainsKey(key))
            return;

        var native = launchOptions[key] as NSDictionary;
        if (native == null)
            return;

        logger.LogDebug("App entry remote notification detected");
        var notification = this.ToNotification(native);
        var dict = native.FromNsDictionary();
        dict.Remove("aps");

        var push = new PushNotification(dict ?? new Dictionary<string, string>(0), notification);
        services.RunDelegates<IPushDelegate>(
            x => x.OnEntry(push),
            logger
        );
    }


    protected virtual void TryProcessIncomingNotification(UNNotification? notification, string logMessage, Action<PushNotification> completionHandler)
    {
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


    protected virtual Notification? ToNotification(NSDictionary data)
    {
        if (data.ContainsKey(apsKey))
        {
            var apsDict = data[apsKey] as NSDictionary;

            if (apsDict?.ContainsKey(alertKey) ?? false)
            {
                var alertDict = apsDict[alertKey] as NSDictionary;
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
