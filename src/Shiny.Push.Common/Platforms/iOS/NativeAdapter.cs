using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Shiny.Push.Infrastructure;
using UIKit;
using UserNotifications;


namespace Shiny.Push
{
    public class NativeAdapter : INativeAdapter
    {
        readonly AppleLifecycle lifecycle;
        readonly IPlatform platform;


        public NativeAdapter(IPlatform platform, AppleLifecycle lifecycle)
        {
            this.platform = platform;
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


        IDisposable? onEntrySub;
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
                }
                else
                {
                    this.onEntrySub = this.lifecycle.RegisterForNotificationReceived(async response =>
                    {
                        if (response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                        {
                            var dict = response.Notification.Request.Content.UserInfo.FromNsDictionary();
                            var data = new PushNotification(dict);
                            //var shiny = response.FromNative();
                            //var pr = new PushNotificationResponse(
                            //    shiny.Notification,
                            //    shiny.ActionIdentifier,
                            //    shiny.Text
                            //);
                            var pushData = new PushNotification(new Dictionary<string, string>());
                            await this.onEntry.Invoke(data).ConfigureAwait(false);
                        }
                    });
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
