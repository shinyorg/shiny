using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Push.Infrastructure;
using UIKit;
using UserNotifications;

namespace Shiny.Push;


public class NativeAdapter : INativeAdapter, IIosLifecycle.IOnFinishedLaunching, IIosLifecycle.IRemoteNotifications
{
    readonly ILogger logger;
    readonly IPlatform platform;
    TaskCompletionSource<NSData>? tokenSource;


    public NativeAdapter(
        IPlatform platform,
        ILogger<NativeAdapter> logger
    )
    {
        this.platform = platform;
        this.logger = logger;
    }


    public Func<string, Task>? OnTokenRefreshed { get; set; }
    public Func<PushNotification, Task>? OnReceived { get; set; }
    public Func<PushNotification, Task>? OnEntry { get; set; }


    public async Task<PushAccessState> RequestAccess()
    {
        var npermission = await this.RequestPermission().ConfigureAwait(false);
        var result = npermission ? AccessState.Available : AccessState.Restricted;
        var deviceToken = await this.RequestRawToken().ConfigureAwait(false);

        string? token = null;
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
        return new PushAccessState(result, token);
    }


    public Task UnRegister() => this.platform
        .InvokeOnMainThreadAsync(UIApplication.SharedApplication.UnregisterForRemoteNotifications);


    public async Task<bool> RequestPermission()
    {
        var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert |
            UNAuthorizationOptions.Badge |
            UNAuthorizationOptions.Sound
        );
        return result.Item1;
    }

    public async Task<NSData> RequestRawToken(CancellationToken cancelToken = default)
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


    static readonly NSString apsKey = new NSString("aps");
    static readonly NSString alertKey = new NSString("alert");

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


    public void OnRegistered(NSData deviceToken) => this.tokenSource?.TrySetResult(deviceToken);
    public void OnFailedToRegister(NSError error) => this.tokenSource?.TrySetException(new Exception(error.LocalizedDescription));
    public async void OnDidReceive(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    {
        if (this.OnReceived == null)
            return;

        var dict = userInfo.FromNsDictionary();
        var data = new PushNotification(dict, null);
        await this.OnReceived.Invoke(data).ConfigureAwait(false);
        completionHandler.Invoke(UIBackgroundFetchResult.NewData);
    }


    public async void Handle(NSDictionary options) 
    {
        if (this.OnEntry == null)
            return;

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
        await this.OnEntry.Invoke(push).ConfigureAwait(false);
    }


    //            //this.onEntrySub.Add(this.lifecycle.RegisterForNotificationReceived(async response =>
    //            //{
    //            //    if (response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
    //            //    {
    //            //        this.logger.LogDebug("Foreground remote notification entry detected");
    //            //        var c = response.Notification.Request.Content;

    //            //        var notification = new Notification(
    //            //            c.Title,
    //            //            c.Body
    //            //        );

    //            //        var dict = c.UserInfo?.FromNsDictionary() ?? new Dictionary<string, string>(0);
    //            //        var data = new PushNotification(dict, notification);
    //            //        await this.onEntry.Invoke(data).ConfigureAwait(false);
    //            //    }
    //            //}));
}
