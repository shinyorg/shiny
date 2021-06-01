using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Notifications;
using Shiny.Infrastructure;


namespace Shiny.Push
{
    public class PushManager : AbstractPushManager, IShinyStartupTask
    {
        readonly Subject<PushNotification> payloadSubj = new Subject<PushNotification>();
        public PushManager(ShinyCoreServices services) : base(services) { }


        public virtual void Start()
        {
            this.Services.Lifecycle.RegisterToReceiveRemoteNotifications(async userInfo =>
            {
                var dict = userInfo.FromNsDictionary();
                var pr = new PushNotification(dict, null);
                await this.Services
                    .Services
                    .SafeResolveAndExecute<IPushDelegate>(x => x.OnReceived(pr))
                    .ConfigureAwait(false);
                this.payloadSubj.OnNext(pr);
            });

            this.Services.Lifecycle.RegisterForNotificationReceived(async response =>
            {
                if (response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                {
                    var shiny = response.FromNative();
                    var pr = new PushNotificationResponse(
                        shiny.Notification,
                        shiny.ActionIdentifier,
                        shiny.Text
                    );

                    await this.Services
                        .Services
                        .RunDelegates<IPushDelegate>(x => x.OnEntry(pr))
                        .ConfigureAwait(false);
                }
            });

            if (!this.CurrentRegistrationToken.IsEmpty())
            {
                // do I need to do this?  I would normally be calling RequestAccess on startup anyhow
                this.RequestAccess().ContinueWith(x => { });
            }
        }


        public override IObservable<PushNotification> WhenReceived() => this.payloadSubj;


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert |
                UNAuthorizationOptions.Badge |
                UNAuthorizationOptions.Sound
            );
            if (!result.Item1)
                return PushAccessState.Denied;

            var deviceToken = await this.RequestDeviceToken(cancelToken).ConfigureAwait(false);
            this.CurrentRegistrationToken = ToTokenString(deviceToken);
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public override async Task UnRegister()
        {
            await this.Services
                .Platform
                .InvokeOnMainThreadAsync(UIApplication.SharedApplication.UnregisterForRemoteNotifications)
                .ConfigureAwait(false);
            this.ClearRegistration();
        }


        protected virtual async Task<NSData> RequestDeviceToken(CancellationToken cancelToken = default)
        {
            var tcs = new TaskCompletionSource<NSData>();
            IDisposable? caller = null;
            try
            {
                //UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(UIRemoteNotificationType.Alert)
                caller = this.Services.Lifecycle.RegisterForRemoteNotificationToken(
                    rawToken => tcs.TrySetResult(rawToken),
                    err => tcs.TrySetException(new Exception(err.LocalizedDescription))
                );
                await this.Services
                    .Platform
                    .InvokeOnMainThreadAsync(
                        () => UIApplication.SharedApplication.RegisterForRemoteNotifications()
                    )
                    .ConfigureAwait(false);
                var rawToken = await tcs.Task;
                var token = ToTokenString(rawToken);
                await this.Services.Services.SafeResolveAndExecute<IPushDelegate>(
                    x => x.OnTokenChanged(token)
                );
                return token;
            }
            finally
            {
                caller?.Dispose();
            }
        }


        protected static string? ToTokenString(NSData deviceToken)
        {
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
    }
}
