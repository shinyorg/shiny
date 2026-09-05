using System;
using Foundation;
using UIKit;
using UserNotifications;

namespace Shiny.Hosting;


/// <summary>
/// Container for iOS/Mac Catalyst lifecycle event interfaces
/// </summary>
public interface IIosLifecycle
{
    /// <summary>
    /// Handles application foreground and background transitions
    /// </summary>
    public interface IApplicationLifecycle
    {
        /// <summary>
        /// Called when the application enters the foreground
        /// </summary>
        void OnForeground();

        /// <summary>
        /// Called when the application enters the background
        /// </summary>
        void OnBackground();
    }

    /// <summary>
    /// Handles the application finished launching event
    /// </summary>
    public interface IOnFinishedLaunching
    {
        /// <summary>
        /// Called when the application has finished launching
        /// </summary>
        /// <param name="args">The launch event arguments</param>
        void Handle(UIApplicationLaunchEventArgs args);
    }

    /// <summary>
    /// Handles remote (push) notification registration and delivery
    /// </summary>
    public interface IRemoteNotifications
    {
        /// <summary>
        /// Called when the device successfully registers for remote notifications
        /// </summary>
        /// <param name="deviceToken">The device token for push notifications</param>
        void OnRegistered(NSData deviceToken);

        /// <summary>
        /// Called when remote notification registration fails
        /// </summary>
        /// <param name="error">The registration error</param>
        void OnFailedToRegister(NSError error);

        /// <summary>
        /// Called when a remote notification is received
        /// </summary>
        /// <param name="userInfo">The notification payload</param>
        /// <param name="completionHandler">The completion handler to call with the fetch result</param>
        void OnDidReceive(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler);
    }

#if !TVOS
    // tvOS notifications only ever update the app icon badge - there is no UNNotificationResponse
    // to hand back, and nothing is ever presented in the foreground
    /// <summary>
    /// Handles local and remote notification presentation and response
    /// </summary>
    public interface INotificationHandler
    {
        /// <summary>
        /// Called when the user interacts with a notification
        /// </summary>
        /// <param name="response">The notification response</param>
        /// <param name="completionHandler">The completion handler to call when finished</param>
        void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler);

        /// <summary>
        /// Called when a notification is about to be presented while the app is in the foreground
        /// </summary>
        /// <param name="notification">The notification to present</param>
        /// <param name="completionHandler">The completion handler to call with presentation options</param>
        void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler);
    }
#endif

    /// <summary>
    /// Handles background URL session events
    /// </summary>
    public interface IHandleEventsForBackgroundUrl
    {
        /// <summary>
        /// Called when events are available for a background URL session
        /// </summary>
        /// <param name="sessionIdentifier">The background session identifier</param>
        /// <param name="completionHandler">The completion handler to call when finished</param>
        /// <returns>True if this handler owns the session identifier</returns>
        bool Handle(string sessionIdentifier, Action completionHandler);
    }

    /// <summary>
    /// Handles NSUserActivity continuation (universal links, Handoff)
    /// </summary>
    public interface IContinueActivity
    {
        /// <summary>
        /// Called when the app should continue a user activity
        /// </summary>
        /// <param name="activity">The user activity to continue</param>
        /// <param name="completionHandler">The restoration handler</param>
        /// <returns>True if this handler can continue the activity</returns>
        bool Handle(NSUserActivity activity, UIApplicationRestorationHandler completionHandler);

        //     ios.SceneWillConnect((scene, sceneSession, sceneConnectionOptions)
        //         => HandleAppLink(sceneConnectionOptions.UserActivities.ToArray()
        //             .FirstOrDefault(a => a.ActivityType == NSUserActivityType.BrowsingWeb)));

        //     ios.SceneContinueUserActivity((scene, userActivity)
        //         => HandleAppLink(userActivity));
    }
}