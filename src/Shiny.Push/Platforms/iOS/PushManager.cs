using System;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Settings;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;

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
            var remoteTask = iOSShinyHost.WhenRegisteredForRemoteNotifications().Take(1).ToTask();
            var tcs = new TaskCompletionSource<AccessState>();

            UNUserNotificationCenter.Current.RequestAuthorization(
                UNAuthorizationOptions.Alert |
                UNAuthorizationOptions.Badge |
                UNAuthorizationOptions.Sound,
                (approved, error) =>
                {
                    if (error != null)
                    {
                        tcs.SetException(new Exception(error.Description));
                        this.CurrentRegistrationToken = null;
                        this.CurrentRegistrationTokenDate = null;
                    }
                    else
                    {
                        var state = approved ? AccessState.Available : AccessState.Denied;
                        tcs.SetResult(state);
                    }
                }
            );
            UIApplication.SharedApplication.RegisterForRemoteNotifications();
            
            await Task.WhenAll(
                remoteTask,
                tcs.Task
            );

            this.CurrentRegistrationToken = ToTokenString(remoteTask.Result);
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            
            return new PushAccessState(tcs.Task.Result, this.CurrentRegistrationToken);
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
