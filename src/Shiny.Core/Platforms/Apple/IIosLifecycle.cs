using System;
using Foundation;
using UIKit;
using UserNotifications;

namespace Shiny.Hosting;


public interface IIosLifecycle
{
    public interface IApplicationLifecycle
    {
        void OnForeground();
        void OnBackground();
    }

    public interface IOnFinishedLaunching
    {
        void Handle(UIApplicationLaunchEventArgs args);
    }

    public interface IRemoteNotifications
    {
        void OnRegistered(NSData deviceToken);
        void OnFailedToRegister(NSError error);
        void OnDidReceive(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler);
    }

    public interface INotificationHandler
    {
        void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler);
        void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler);
    }

    public interface IHandleEventsForBackgroundUrl
    {
        bool Handle(string sessionIdentifier, Action completionHandler);
    }

    public interface IContinueActivity
    {
        bool Handle(NSUserActivity activity, UIApplicationRestorationHandler completionHandler);

        //     ios.SceneWillConnect((scene, sceneSession, sceneConnectionOptions)
        //         => HandleAppLink(sceneConnectionOptions.UserActivities.ToArray()
        //             .FirstOrDefault(a => a.ActivityType == NSUserActivityType.BrowsingWeb)));

        //     ios.SceneContinueUserActivity((scene, userActivity)
        //         => HandleAppLink(userActivity));
    }
}