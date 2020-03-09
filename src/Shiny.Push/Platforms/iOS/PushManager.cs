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


namespace Shiny.Push
{
    public class PushManager : AbstractPushManager
    {
        readonly Subject<IDictionary<string, string>> pushSubject;
        Subject<NSData>? onToken;


        public PushManager(ISettings settings, IServiceProvider services) : base(settings)
        {
            this.pushSubject = new Subject<IDictionary<string, string>>();
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
                    var dict = nsdict.FromNsDictionary();
                    await services.SafeResolveAndExecute<IPushDelegate>(x => x.OnReceived(dict));
                    action(UIBackgroundFetchResult.NewData);
                }
            );
        }


        public override IObservable<IDictionary<string, string>> WhenReceived() => this.pushSubject;


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var deviceToken = await this.RequestDeviceToken(cancelToken);
            this.CurrentRegistrationToken = ToTokenString(deviceToken);
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public override Task UnRegister()
            => Dispatcher.InvokeOnMainThreadAsync(UIApplication.SharedApplication.UnregisterForRemoteNotifications);            


        protected virtual async Task<NSData> RequestDeviceToken(CancellationToken cancelToken = default)
        {
            this.onToken = new Subject<NSData>();
            var remoteTask = this.onToken.Take(1).ToTask(cancelToken);

            var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert |
                UNAuthorizationOptions.Badge |
                UNAuthorizationOptions.Sound
            );
            if (!result.Item1)
                throw new Exception(result.Item2.LocalizedDescription);

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
