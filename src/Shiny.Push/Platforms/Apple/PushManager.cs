using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using UIKit;
using UserNotifications;

namespace Shiny.Push;


public class PushManager : NotifyPropertyChanged, IPushManager, IIosLifecycle.IOnFinishedLaunching, IIosLifecycle.IRemoteNotifications, IIosLifecycle.INotificationHandler
{
    static readonly NSString apsKey = new NSString("aps");
    static readonly NSString alertKey = new NSString("alert");

    readonly IPlatform platform;
    readonly ILogger logger;
    readonly IPushProvider? provider;
    readonly IEnumerable<IPushDelegate> delegates;
    TaskCompletionSource<NSData>? tokenSource;


    public PushManager(
        IPlatform platform,
        ILogger<PushManager> logger,
        IPushProvider? provider,
        IEnumerable<IPushDelegate> delegates
    )
    {
        this.platform = platform;
        this.logger = logger;
        this.provider = provider;
        this.delegates = delegates;
    }


    public IPushTagSupport? Tags => this.provider?.Tags;

    string? registrationToken;
    public string? RegistrationToken
    {
        get => this.registrationToken;
        set => this.Set(ref this.registrationToken, value);
    }


    public async void Start()
    {
        if (this.RegistrationToken.IsEmpty())
            return;

        try
        {
            // TODO: this will already have stored, could monitor the change?
            var result = await this.RequestAccess().ConfigureAwait(false);
            if (result.Status == AccessState.Available)
            {

            }
            // TODO: call OnTokenChanged event if provider token changes (or native if no provider)
        }
        catch (Exception ex)
        {
            this.logger.LogWarning("Failed to auto start push", ex);
        }
    }


    public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
    {
        //        // TODO: make this configurable
        //        var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
        //            UNAuthorizationOptions.Alert |
        //            UNAuthorizationOptions.Badge |
        //            UNAuthorizationOptions.Sound
        //        //UNAuthorizationOptions.CarPlay
        //        );
        //        return result.Item1;

        //var npermission = await this.RequestPermission().ConfigureAwait(false);
        //var result = npermission ? AccessState.Available : AccessState.Restricted;
        var deviceToken = await this.RequestRawToken(cancelToken).ConfigureAwait(false);

        if (this.provider != null)
            await this.provider.Register(deviceToken);

        // TODO: store provider & native tokens
        var token = this.FromNative(deviceToken);
        return new PushAccessState(AccessState.Available, token);
    }


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


    protected string FromNative(NSData deviceToken)
    {
        string token = null!;
        if (deviceToken.Length > 0)
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                var data = deviceToken.ToArray();
                token = BitConverter
                    .ToString(data)
                    .Replace("-", "")
                    .Replace("\"", "");
            }
            else if (!deviceToken.Description.IsEmpty())
            {
                token = deviceToken.Description.Trim('<', '>');
            }
        }
        return token;
    }
    public void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler) { }
    public void OnRegistered(NSData deviceToken) => this.tokenSource?.TrySetResult(deviceToken);
    public void OnFailedToRegister(NSError error) => this.tokenSource?.TrySetException(new Exception(error.LocalizedDescription));
    public async void OnDidReceive(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    {
        //if (this.OnReceived == null)
        //    return;

        var dict = userInfo.FromNsDictionary();
        var data = new PushNotification(dict, null);
        //await this.OnReceived.Invoke(data).ConfigureAwait(false);
        completionHandler.Invoke(UIBackgroundFetchResult.NewData);
    }



    public async void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler)
    {
        //if (this.OnEntry == null)
        //    return;

        //if (response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
        //{
        //    this.logger.LogDebug("Foreground remote notification entry detected");
        //    var c = response.Notification.Request.Content;

        //    var notification = new Notification(
        //        c.Title,
        //        c.Body
        //    );

        //    var dict = c.UserInfo?.FromNsDictionary() ?? new Dictionary<string, string>(0);
        //    var data = new PushNotification(dict, notification);
        //    await this.OnEntry.Invoke(data).ConfigureAwait(false);
        //    completionHandler();
        //}
    }


    public async void Handle(NSDictionary options)
    {
        //if (this.OnEntry == null)
        //    return;

        if (!options.ContainsKey(UIApplication.LaunchOptionsRemoteNotificationKey))
            return;

        this.logger.LogDebug("App entry remote notification detected");
        var data = options[UIApplication.LaunchOptionsRemoteNotificationKey] as NSDictionary;

        Notification? notification = null;
        IDictionary<string, string>? dict = null;

        if (data != null)
        {
            notification = this.ToNotification(data);
            dict = data.FromNsDictionary();
            dict.Remove("aps");
        }
        var push = new PushNotification(dict ?? new Dictionary<string, string>(0), notification);
        //await this.OnEntry.Invoke(push).ConfigureAwait(false);
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

//public static async Task<PermissionStatus> RequestAsync(UNAuthorizationOptions authorizationOptions)
//{
//    var (_, error) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(authorizationOptions);
//    if (error != null)
//    {
//        throw new NSErrorException(error);
//    }

//    return await CheckStatusAsync();
//}

//public static async Task<PermissionStatus> CheckStatusAsync()
//{
//    var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
//    var authorizationStatus = settings.AuthorizationStatus;
//    return authorizationStatus switch
//    {
//        UNAuthorizationStatus.NotDetermined => PermissionStatus.Unknown,
//        UNAuthorizationStatus.Denied => PermissionStatus.Denied,
//        UNAuthorizationStatus.Authorized => PermissionStatus.Granted,
//        UNAuthorizationStatus.Provisional => PermissionStatus.Restricted,
//        UNAuthorizationStatus.Ephemeral => PermissionStatus.Granted,
//        _ => throw new ArgumentOutOfRangeException(
//            paramName: nameof(UNNotificationSettings.AuthorizationStatus),
//            actualValue: authorizationStatus,
//            message: null),
//    };
//}
//        }

//        public partial class PushNotification : BasePlatformPermission
//{
//    public override Task<PermissionStatus> RequestAsync()
//    {
//        EnsureDeclared();
//        EnsureMainThread();
//        return PushNotificationPermissions.RequestAsync(UNAuthorizationOptions.Alert |
//                                                        UNAuthorizationOptions.Sound |
//                                                        UNAuthorizationOptions.Badge);
//    }

//    public override Task<PermissionStatus> CheckStatusAsync()
//    {
//        EnsureDeclared();
//        return PushNotificationPermissions.CheckStatusAsync();
//    }
//}

//public partial class ProvisionalPushNotification : BasePlatformPermission
//{
//    public override Task<PermissionStatus> RequestAsync()
//    {
//        EnsureDeclared();
//        EnsureMainThread();
//        return PushNotificationPermissions.RequestAsync(UNAuthorizationOptions.Alert |
//                                                        UNAuthorizationOptions.Sound |
//                                                        UNAuthorizationOptions.Badge |
//                                                        UNAuthorizationOptions.Provisional);
//    }

//    public override Task<PermissionStatus> CheckStatusAsync()
//    {
//        EnsureDeclared();
//        return PushNotificationPermissions.CheckStatusAsync();
//    }
//}