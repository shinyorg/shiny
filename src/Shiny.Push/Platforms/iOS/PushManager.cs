using System;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Settings;
using Shiny.Logging;


namespace Shiny.Push
{
    //https://docs.microsoft.com/en-us/xamarin/ios/platform/user-notifications/enhanced-user-notifications?tabs=windows
    public class PushManager : AbstractPushManager
    {
        public PushManager(ISettings settings) : base(settings)
        {
            UNUserNotificationCenter.Current.Delegate = new ShinyPushDelegate();
        }


        public override async Task<PushAccessState> RequestAccess()
        {
            // TODO: timeout & cancellation
            var result = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert |
                UNAuthorizationOptions.Badge |
                UNAuthorizationOptions.Sound
            );
            if (!result.Item1)
            {
                Log.Write("PushNotification", result.Item2.LocalizedDescription);
                return new PushAccessState(AccessState.Unknown, null);
            }
            var remoteTask = iOSShinyHost.WhenRegisteredForRemoteNotifications().Take(1).ToTask();
            UIApplication.SharedApplication.RegisterForRemoteNotifications();

            var deviceToken = await remoteTask;
            this.CurrentRegistrationToken = ToTokenString(deviceToken);
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public override Task UnRegister()
        {
            UIApplication.SharedApplication.UnregisterForRemoteNotifications();
            return Task.CompletedTask;
        }


        protected static string ToTokenString(NSData deviceToken)
        {
            var token = "";
            if (!deviceToken.Description.IsEmpty())            
                token = deviceToken.Description.Trim('<', '>');

            return token;
        }
    }
}
