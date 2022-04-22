using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Foundation;

using Microsoft.Extensions.Logging;

using Shiny.Push.Infrastructure;
using UIKit;
using UserNotifications;


namespace Shiny.Push
{
    public class NativeAdapter : INativeAdapter
    {
        readonly AppleLifecycle lifecycle;
        readonly ILogger logger;
        readonly IPlatform platform;


        public NativeAdapter(
            IPlatform platform,
            ILogger<NativeAdapter> logger,
            AppleLifecycle lifecycle
        )
        {
            this.platform = platform;
            this.logger = logger;
            this.lifecycle = lifecycle;
        }


        IDisposable? onReceviedSub;
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
                    this.onReceviedSub = this.lifecycle.RegisterToReceiveRemoteNotifications(async userInfo =>
                    {
                        var dict = userInfo.FromNsDictionary();
                        var data = new PushNotification(dict);
                        await this.onReceived.Invoke(data).ConfigureAwait(false);
                    });
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

                    //UIApplication.SharedApplication.IsRegisteredForRemoteNotifications
                    this.onEntrySub.Add(this.lifecycle.RegisterForOnFinishedLaunching(async options =>
                    {
                        if (options.ContainsKey(UIApplication.LaunchOptionsRemoteNotificationKey))
                        {
                            this.logger.LogDebug("App entry remote notification detected");
                            var data = options[UIApplication.LaunchOptionsRemoteNotificationKey] as NSDictionary;
                            var dict = data?.FromNsDictionary() ?? new Dictionary<string, string>(0);
                            var push = new PushNotification(dict);
                            await this.onEntry.Invoke(push).ConfigureAwait(false);
                        }
                    }));

                    this.onEntrySub.Add(this.lifecycle.RegisterForNotificationReceived(async response =>
                    {
                        if (response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                        {
                            this.logger.LogDebug("Foreground remote notification entry detected");
                            var dict = response.Notification.Request.Content.UserInfo.FromNsDictionary();
                            var data = new PushNotification(dict);
                            await this.onEntry.Invoke(data).ConfigureAwait(false);
                        }
                    }));
                }
            }
        }


        public Func<string, Task>? OnTokenRefreshed { get; set; }


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
                //UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(UIRemoteNotificationType.Alert)
                caller = this.lifecycle.RegisterForRemoteNotificationToken(
                    rawToken => tcs.TrySetResult(rawToken),
                    err => tcs.TrySetException(new Exception(err.LocalizedDescription))
                );
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
    }
}
