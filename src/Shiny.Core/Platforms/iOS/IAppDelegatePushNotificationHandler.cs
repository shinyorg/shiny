using System;
using Foundation;
using UIKit;


namespace Shiny
{
    public interface IAppDelegatePushNotificationHandler
    {
        void DidReceiveRemoteNotification(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler);
        void RegisteredForRemoteNotifications(NSData deviceToken);
        void FailedToRegisterForRemoteNotifications(NSError error);
    }
}