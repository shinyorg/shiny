using System;
using Foundation;
using UserNotifications;

namespace Shiny.Hosting;


/// <summary>
/// Container for macOS (AppKit) lifecycle event interfaces
/// </summary>
public interface IMacLifecycle
{
    /// <summary>
    /// Handles application become-active and resign-active transitions
    /// </summary>
    public interface IApplicationLifecycle
    {
        /// <summary>
        /// Called when the application becomes active (foreground)
        /// </summary>
        void OnForeground();

        /// <summary>
        /// Called when the application resigns active (background)
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
        /// <param name="launchOptions">The user info dictionary from NSApplicationDidFinishLaunching, may be null</param>
        void Handle(NSDictionary? launchOptions);
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
        void OnDidReceive(NSDictionary userInfo);
    }


    /// <summary>
    /// Handles local and remote notification presentation and response
    /// </summary>
    public interface INotificationHandler
    {
        /// <summary>
        /// Called when the user interacts with a notification
        /// </summary>
        void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler);

        /// <summary>
        /// Called when a notification is about to be presented while the app is in the foreground
        /// </summary>
        void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler);
    }


    /// <summary>
    /// Handles NSUserActivity continuation (universal links, Handoff)
    /// </summary>
    public interface IContinueActivity
    {
        /// <summary>
        /// Called when the app should continue a user activity
        /// </summary>
        bool Handle(NSUserActivity activity);
    }
}
