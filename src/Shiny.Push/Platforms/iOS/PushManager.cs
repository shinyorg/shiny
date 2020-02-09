using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Settings;


namespace Shiny.Push
{
    //https://docs.microsoft.com/en-us/xamarin/ios/platform/user-notifications/enhanced-user-notifications?tabs=windows
    public class PushManager : AbstractPushManager
    {
        public PushManager(ISettings settings) : base(settings)
        {
            UNUserNotificationCenter.Current.Delegate = new ShinyPushDelegate();
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
