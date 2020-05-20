using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Settings;
using Shiny.Notifications;


namespace Shiny.Push
{
    public class PushManager : AbstractPushManager, IShinyStartupTask
    {
        readonly iOSNotificationDelegate nativeDelegate;
        readonly IServiceProvider services;
        readonly Subject<IDictionary<string, string>> payloadSubj;
        Subject<NSData>? onToken;


        public PushManager(ISettings settings,
                           IServiceProvider services,
                           iOSNotificationDelegate nativeDelegate) : base(settings)
        {
            this.services = services;
            this.nativeDelegate = nativeDelegate;

            this.payloadSubj = new Subject<IDictionary<string, string>>();
        }


        public void Start()
        {
            this.nativeDelegate
                .WhenPresented()
                .Where(x => x.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                .SubscribeAsync(async x =>
                {
                    var payload = x.Notification.Request?.Content?.UserInfo?.FromNsDictionary();
                    await this.services
                        .RunDelegates<IPushDelegate>(x => x.OnReceived(payload))
                        .ConfigureAwait(false);

                    this.payloadSubj.OnNext(payload);
                    x.CompletionHandler?.Invoke(UNNotificationPresentationOptions.Alert);
                });

            this.nativeDelegate
                .WhenResponse()
                .Where(x => x.Response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                .SubscribeAsync(async x =>
                {
                    // TODO: get tap response
                    x.CompletionHandler?.Invoke();
                });

            iOSShinyHost.RegisterForRemoteNotifications(
                async deviceToken =>
                {
                    this.onToken?.OnNext(deviceToken);
                    await services.SafeResolveAndExecute<IPushDelegate>(x =>
                    {
                        var stoken = ToTokenString(deviceToken);
                        return x.OnTokenChanged(stoken);
                    });
                },
                e => this.onToken?.OnError(new Exception(e.LocalizedDescription)),
                async (nsdict, action) =>
                {
                    // this will only be fired while in the bg, so don't fire observable
                    var dict = nsdict.FromNsDictionary();
                    await services.SafeResolveAndExecute<IPushDelegate>(x => x.OnReceived(dict));
                    action(UIBackgroundFetchResult.NewData);
                }
            );

            // this will be on the main thread already
            if (!this.CurrentRegistrationToken.IsEmpty())
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
        }


        public override IObservable<IDictionary<string, string>> WhenReceived() => this.payloadSubj;


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
            this.onToken = new Subject<NSData>();
            var remoteTask = this.onToken.Take(1).ToTask(cancelToken);
            await Dispatcher.InvokeOnMainThreadAsync(UIApplication.SharedApplication.RegisterForRemoteNotifications);
            var data = await remoteTask;
            return data;
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
