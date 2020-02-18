using System;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Settings;
using System.Runtime.InteropServices;

namespace Shiny.Push
{
    //https://docs.microsoft.com/en-us/xamarin/ios/platform/user-notifications/enhanced-user-notifications?tabs=windows
    public class PushManager : AbstractPushManager
    {
        static TaskCompletionSource<string>? RemoteTokenTask;



        public PushManager(ISettings settings) : base(settings)
        {
            UNUserNotificationCenter.Current.Delegate = new ShinyPushDelegate();
        }


        public override async Task<PushAccessState> RequestAccess()
        {
            // TODO: error on pending request?
            //if (!UIApplication.SharedApplication.IsRegisteredForRemoteNotifications)
            //{
                RemoteTokenTask = new TaskCompletionSource<string>();
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            //}

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

            await Task.WhenAll(
                RemoteTokenTask.Task,
                tcs.Task
            );

            this.CurrentRegistrationToken = RemoteTokenTask.Task.Result;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;

            // TODO: timeout?
            return new PushAccessState(tcs.Task.Result, RemoteTokenTask.Task.Result);
        }


        public static void RegisteredForRemoteNotifications(NSData deviceToken)
        {
            if(UIDevice.CurrentDevice.CheckSystemVersion(13,0))
            {
                if(deviceToken.Length > 0)
                {
                    var result = new byte[deviceToken.Length];
                    Marshal.Copy(deviceToken.Bytes, result, 0, (int)deviceToken.Length);
                    var token = BitConverter.ToString(result).Replace("-", string.Empty);
                    RemoteTokenTask?.SetResult(token);
                }
            }
            else if (!deviceToken.Description.IsEmpty())
            {
                var token = deviceToken.Description.Trim('<', '>');
                RemoteTokenTask?.SetResult(token);
            }
            // TODO: if empty returned? old token vs new token?
            // TODO: store it in settings?
        }


        public static void FailedToRegisterForRemoteNotifications(NSError error)
        {
            Console.WriteLine("Failed to register for remote notifications - " + error.LocalizedDescription);
            RemoteTokenTask?.SetException(new ArgumentException(error.LocalizedDescription));
        }
    }
}
