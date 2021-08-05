using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;

using UserNotifications;

namespace Shiny.Push
{
    public class ApnManager
    {
        readonly IPlatform platform;
        readonly AppleLifecycle lifecycle;


        public ApnManager(IPlatform platform, AppleLifecycle lifecycle)
        {
            this.platform = platform;
            this.lifecycle = lifecycle;
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

        public async Task<string?> RequestToken(CancellationToken cancellationToken = default)
        {
            var deviceToken = await this.RequestRawToken(cancellationToken).ConfigureAwait(false);
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
            return token;
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
