using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using UIKit;

namespace Shiny.Push;


public class PushManager : NotifyPropertyChanged, IPushManager, IIosLifecycle.IOnFinishedLaunching, IIosLifecycle.IRemoteNotifications, IIosLifecycle.INotificationHandler
{
    static readonly NSString apsKey = new NSString("aps");
    static readonly NSString alertKey = new NSString("alert");

    readonly ILogger logger;
    readonly IPlatform platform;
    //    TaskCompletionSource<NSData>? tokenSource;

    public PushManager(ILogger<PushManager> logger)
    {
        this.logger = logger;
    }

    public IPushTagSupport? Tags => throw new NotImplementedException();

    public string? RegistrationToken => throw new NotImplementedException();

    //public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
    //public string? CurrentRegistrationToken => this.container.CurrentRegistrationToken;
    //public string[]? RegisteredTags => this.container.RegisteredTags;


    //public async void Start()
    //{
    //    this.adapter.OnReceived = push => this.container.OnReceived(push);
    //    this.adapter.OnEntry = push => this.container.OnEntry(push);

    //    await this.container
    //        .TryAutoStart(this.adapter, this.logger)
    //        .ConfigureAwait(false);
    //}


    public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
    {
        //        var npermission = await this.RequestPermission().ConfigureAwait(false);
        //        var result = npermission ? AccessState.Available : AccessState.Restricted;
        //        var deviceToken = await this.RequestRawToken().ConfigureAwait(false);

        //        this.tokenSource = new();
        //        using var cancelSrc = cancelToken.Register(() => this.tokenSource.TrySetCanceled());

        //        await this.platform
        //            .InvokeOnMainThreadAsync(
        //                () => UIApplication
        //                    .SharedApplication
        //                    .RegisterForRemoteNotifications()
        //            )
        //            .ConfigureAwait(false);

        //        var rawToken = await this.tokenSource.Task.ConfigureAwait(false);
        //        return rawToken;


        //        string? token = null;
        //        if (deviceToken.Length > 0)
        //        {
        //            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
        //            {
        //                var data = deviceToken.ToArray();
        //                token = BitConverter
        //                    .ToString(data)
        //                    .Replace("-", "")
        //                    .Replace("\"", "");
        //            }
        //            else if (!deviceToken.Description.IsEmpty())
        //            {
        //                token = deviceToken.Description.Trim('<', '>');
        //            }
        //        }



        //        // TODO: make this configurable
        //        var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
        //            UNAuthorizationOptions.Alert |
        //            UNAuthorizationOptions.Badge |
        //            UNAuthorizationOptions.Sound
        //        //UNAuthorizationOptions.CarPlay
        //        );
        //        return result.Item1;
        return PushAccessState.Denied;
    }


    public async Task UnRegister()
    {
        //this.platform.InvokeOnMainThreadAsync(UIApplication.SharedApplication.UnregisterForRemoteNotifications);
    }

    //    public void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler) { }
    //    public void OnRegistered(NSData deviceToken) => this.tokenSource?.TrySetResult(deviceToken);
    //    public void OnFailedToRegister(NSError error) => this.tokenSource?.TrySetException(new Exception(error.LocalizedDescription));
    //    public async void OnDidReceive(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    //    {
    //        if (this.OnReceived == null)
    //            return;

    //        var dict = userInfo.FromNsDictionary();
    //        var data = new PushNotification(dict, null);
    //        await this.OnReceived.Invoke(data).ConfigureAwait(false);
    //        completionHandler.Invoke(UIBackgroundFetchResult.NewData);
    //    }



    //    public async void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler)
    //    {
    //        if (this.OnEntry == null)
    //            return;

    //        if (response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
    //        {
    //            this.logger.LogDebug("Foreground remote notification entry detected");
    //            var c = response.Notification.Request.Content;

    //            var notification = new Notification(
    //                c.Title,
    //                c.Body
    //            );

    //            var dict = c.UserInfo?.FromNsDictionary() ?? new Dictionary<string, string>(0);
    //            var data = new PushNotification(dict, notification);
    //            await this.OnEntry.Invoke(data).ConfigureAwait(false);
    //            completionHandler();
    //        }
    //    }


    //    public async void Handle(NSDictionary options)
    //    {
    //        if (this.OnEntry == null)
    //            return;

    //        if (!options.ContainsKey(UIApplication.LaunchOptionsRemoteNotificationKey))
    //            return;

    //        this.logger.LogDebug("App entry remote notification detected");
    //        var data = options[UIApplication.LaunchOptionsRemoteNotificationKey] as NSDictionary;

    //        Notification? notification = null;
    //        IDictionary<string, string>? dict = null;

    //        if (data != null)
    //        {
    //            notification = this.ToNotification(data);
    //            dict = data.FromNsDictionary();
    //            dict.Remove("aps");
    //        }
    //        var push = new PushNotification(dict ?? new Dictionary<string, string>(0), notification);
    //        await this.OnEntry.Invoke(push).ConfigureAwait(false);
    //    }



    //    protected virtual Notification? ToNotification(NSDictionary data)
    //    {
    //        if (data.ContainsKey(apsKey))
    //        {
    //            var apsDict = data[apsKey] as NSDictionary;

    //            if (apsDict?.ContainsKey(alertKey) ?? false)
    //            {
    //                var alertDict = apsDict[alertKey] as NSDictionary;
    //                // category
    //                if (alertDict != null)
    //                {
    //                    return new Notification(
    //                        alertDict["title"]?.ToString(),
    //                        alertDict["body"]?.ToString()
    //                    );
    //                }
    //            }
    //        }
    //        return null;
    //    }
}