using System;
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


public class NativeAdapter : INativeAdapter, IIosLifecycle.IRemoteNotifications
{
    readonly ILogger logger;
    readonly IPlatform platform;


    public NativeAdapter(
        IPlatform platform,
        ILogger<NativeAdapter> logger
    )
    {
        this.platform = platform;
        this.logger = logger;
    }

    
    public Func<string, Task>? OnTokenRefreshed { get; set; }


    IDisposable ? onReceviedSub;
    Func<PushNotification, Task>? onReceived;
    public Func<PushNotification, Task>? OnReceived
    {
        get => this.onReceived;
        set
        {
            this.onReceived = value;
            if (this.onReceived == null)
            {
                this.onReceviedSub?.Dispose();
            }
            else
            {
                //this.onReceviedSub = this.lifecycle.RegisterToReceiveRemoteNotifications(async userInfo =>
                //{
                //    var dict = userInfo.FromNsDictionary();
                //    var data = new PushNotification(dict, null);
                //    await this.onReceived.Invoke(data).ConfigureAwait(false);
                //});
            }
        }
    }


    CompositeDisposable? onEntrySub;
    Func<PushNotification, Task>? onEntry;
    public Func<PushNotification, Task>? OnEntry
    {
        get => this.onEntry;
        set
        {
            this.onEntry = value;
            if (this.onEntry == null)
            {
                this.onEntrySub?.Dispose();
                this.onEntrySub = null;
            }
            else
            {
                this.onEntrySub ??= new CompositeDisposable();
                
                //this.onEntrySub.Add(this.lifecycle.RegisterForOnFinishedLaunching(async options =>
                //{
                //    if (options.ContainsKey(UIApplication.LaunchOptionsRemoteNotificationKey))
                //    {
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
                //        await this.onEntry.Invoke(push).ConfigureAwait(false);
                //    }
                //}));

                //this.onEntrySub.Add(this.lifecycle.RegisterForNotificationReceived(async response =>
                //{
                //    if (response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                //    {
                //        this.logger.LogDebug("Foreground remote notification entry detected");
                //        var c = response.Notification.Request.Content;

                //        var notification = new Notification(
                //            c.Title,
                //            c.Body
                //        );

                //        var dict = c.UserInfo?.FromNsDictionary() ?? new Dictionary<string, string>(0);
                //        var data = new PushNotification(dict, notification);
                //        await this.onEntry.Invoke(data).ConfigureAwait(false);
                //    }
                //}));
            }
        }
    }        


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
        var tcs = new TaskCompletionSource<NSData>();
        IDisposable? caller = null;
        try
        {
            //caller = this.lifecycle.RegisterForRemoteNotificationToken(
            //    rawToken => tcs.TrySetResult(rawToken),
            //    err => tcs.TrySetException(new Exception(err.LocalizedDescription))
            //);
            await this.platform
                .InvokeOnMainThreadAsync(
                    () => UIApplication.SharedApplication.RegisterForRemoteNotifications()
                )
                .ConfigureAwait(false);
            var rawToken = await tcs.Task;
            return rawToken;
        }
        finally
        {
            caller?.Dispose();
        }
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

    public void OnRegistered(NSData deviceToken) => throw new NotImplementedException();
}
