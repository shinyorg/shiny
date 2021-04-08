using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
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
        readonly iOSNotificationDelegate nativeDelegate;
        readonly Subject<PushNotification> payloadSubj;
        readonly AppleLifecycle lifecycle;


        public PushManager(ShinyCoreServices services, AppleLifecycle lifecycle, iOSNotificationDelegate nativeDelegate) : base(services)
        {
            this.nativeDelegate = nativeDelegate;
            this.lifecycle = lifecycle;
            this.payloadSubj = new Subject<PushNotification>();
        }


        public virtual void Start()
        {
            this.lifecycle.RegisterToReceiveRemoteNotifications(async userInfo =>
            {
                var dict = userInfo.FromNsDictionary();
                var pr = new PushNotification(dict, null);
                await this.Services.Services.SafeResolveAndExecute<IPushDelegate>(x => x.OnReceived(pr));
                //completionHandler(UIBackgroundFetchResult.NewData);
            });

            this.nativeDelegate
                .WhenPresented()
                .Where(x => x.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                .SubscribeAsync(async x =>
                {
                    var notification = x.Notification.Request.FromNative();
                    var push = new PushNotification(notification.Payload, notification);

                    await this.Services
                        .Services
                        .RunDelegates<IPushDelegate>(x => x.OnReceived(push))
                        .ConfigureAwait(false);

                    this.payloadSubj.OnNext(push);
                    x.CompletionHandler?.Invoke(UNNotificationPresentationOptions.Alert);
                });

            this.nativeDelegate
                .WhenResponse()
                .Where(x => x.Response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                .SubscribeAsync(async x =>
                {
                    var response = x.Response.FromNative();
                    var pr = new PushNotificationResponse(
                        response.Notification,
                        response.ActionIdentifier,
                        response.Text
                    );

                    await this.Services
                        .Services
                        .RunDelegates<IPushDelegate>(x => x.OnEntry(pr))
                        .ConfigureAwait(false);
                    x.CompletionHandler();
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

            var deviceToken = await this.RequestDeviceToken(cancelToken);
            this.CurrentRegistrationToken = ToTokenString(deviceToken);
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public override async Task UnRegister()
        {
            await Dispatcher.InvokeOnMainThreadAsync(UIApplication.SharedApplication.UnregisterForRemoteNotifications);
            this.ClearRegistration();
        }


        protected virtual async Task<NSData> RequestDeviceToken(CancellationToken cancelToken = default)
        {
            var tcs = new TaskCompletionSource<NSData>();
            IDisposable? caller = null;
            try
            {
                caller = this.lifecycle.RegisterForRemoteNotificationToken(
                    nsdata => tcs.TrySetResult(nsdata),
                    err => tcs.TrySetException(new Exception(err.LocalizedDescription))
                );
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
