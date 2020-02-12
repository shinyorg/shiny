using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Settings;
using Shiny.Notifications;
using Shiny.Logging;


namespace Shiny.Push
{
    public class PushManager : AbstractPushManager, IShinyStartupTask
    {
        readonly ShinyNotificationContext context;


        public PushManager(ShinyNotificationContext context, ISettings settings) : base(settings)
        {
            this.context = context;
        }


        public void Start()
        {
            var sdelegate = this.context.Services.Resolve<IPushDelegate>();

            this.context
                .WhenWillPresentNotification()
                .Where(x => x.Notification.Request.Trigger is UNPushNotificationTrigger)
                .Subscribe(x =>
                    Log.SafeExecute(() =>
                    {
                        sdelegate.OnReceived();
                        x.CompletionHandler(UNNotificationPresentationOptions.None);
                    })
                );
        }


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
            var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert |
                UNAuthorizationOptions.Badge |
                UNAuthorizationOptions.Sound
            );
            if (!result.Item1)
                throw new Exception(result.Item2.LocalizedDescription);

            var remoteTask = iOSShinyHost.WhenRegisteredForRemoteNotifications().Take(1).ToTask(cancelToken);
            await Dispatcher.InvokeOnMainThreadAsync(UIApplication.SharedApplication.RegisterForRemoteNotifications);

            var data = await remoteTask;
            return data;
        }


        protected static string? ToTokenString(NSData deviceToken)
        {
            var data = deviceToken.ToArray();            
            var token = BitConverter
                .ToString(data)
                .Replace("-", "")
                .Replace("\"", "");

            return token;
        }
    }
}
