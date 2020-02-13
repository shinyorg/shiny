using System;
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
using Shiny.Logging;


namespace Shiny.Push
{
    public class PushManager : AbstractPushManager, IShinyStartupTask
    {
        readonly Subject<IPushNotification> pushSubject;
        readonly ShinyNotificationContext context;


        // launch options?  UIApplication.LaunchOptionsRemoteNotificationKey
        public PushManager(ShinyNotificationContext context, ISettings settings) : base(settings)
        {
            this.pushSubject = new Subject<IPushNotification>();
            this.context = context;
        }


        public void Start()
        {
            var sdelegate = this.context.Services.Resolve<IPushDelegate>();

            this.context
                .WhenWillPresentNotification()
                .Where(x => x.Notification.Request.Trigger is UNPushNotificationTrigger)
                .Subscribe(async x =>
                {
                    try
                    {
                        var push = new PushNotification(x.Notification);
                        await sdelegate.OnReceived(push);
                        this.pushSubject.OnNext(push);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                    }
                    finally
                    {
                        x.CompletionHandler(UNNotificationPresentationOptions.None);
                    }
                });
        }


        public override IObservable<IPushNotification> WhenReceived() => this.pushSubject;


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
