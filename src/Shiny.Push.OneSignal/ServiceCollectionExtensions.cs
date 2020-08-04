using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseOneSignalPush(this IServiceCollection services,
                                            Type delegateType,
                                            params NotificationCategory[] categories)
        {
#if NETSTANDARD2_0
            return false;
#else
            //services.RegisterModule(new PushModule(
            //    typeof(Shiny.Push.AzureNotificationHubs.PushManager),
            //    delegateType,
            //    categories
            //));
            //services.AddSingleton(new Shiny.Push.AzureNotificationHubs.AzureNotificationConfig(listenerConnectionString, hubName));
            return true;
#endif
        }


        public static bool UseOneSignalPush<TPushDelegate>(this IServiceCollection services,
                                                           params NotificationCategory[] categories)
            where TPushDelegate : class, IPushDelegate
            => services.UseOneSignalPush(
                typeof(TPushDelegate),
                categories
            );
    }
}

//https://documentation.onesignal.com/docs/xamarin-sdk-setup

//<application....>

//  <receiver android:name="com.onesignal.GcmBroadcastReceiver"
//            android:permission="com.google.android.c2dm.permission.SEND" >
//    <intent-filter>
//      <action android:name="com.google.android.c2dm.intent.RECEIVE" />
//      <category android:name="${manifestApplicationId}" />
//    </intent-filter>
//  </receiver>

//</application>
//MainPage = new OneSignalXamarinFormsExamplePage();

////Remove this method to stop OneSignal Debugging
//OneSignal.Current.SetLogLevel(LOG_LEVEL.VERBOSE, LOG_LEVEL.NONE);

//  OneSignal.Current.StartInit("YOUR_ONESIGNAL_APP_ID")
//  .Settings(new Dictionary<string, bool>() {
//    { IOSSettings.kOSSettingsKeyAutoPrompt, false },
//    { IOSSettings.kOSSettingsKeyInAppLaunchURL, false } })
//  .InFocusDisplaying(OSInFocusDisplayOption.Notification)
//  .EndInit();

//// The promptForPushNotificationsWithUserResponse function will show the iOS push notification prompt. We recommend removing the following code and instead using an In-App Message to prompt for notification permission (See step 7)
//OneSignal.Current.RegisterForPushNotifications();




//using System;
//using Foundation;
//using UIKit;
//using UserNotifications;
//using Com.OneSignal;
//using Com.OneSignal.Abstractions;

//namespace OneSignalNotificationServiceExtension
//{
//    [Register("NotificationService")]
//    public class NotificationService : UNNotificationServiceExtension
//    {
//        Action<UNNotificationContent> ContentHandler { get; set; }
//        UNMutableNotificationContent BestAttemptContent { get; set; }
//        UNNotificationRequest ReceivedRequest { get; set; }

//        protected NotificationService(IntPtr handle) : base(handle)
//        {
//            // Note: this .ctor should not contain any initialization logic.
//        }

//        public override void DidReceiveNotificationRequest(UNNotificationRequest request, Action<UNNotificationContent> contentHandler)
//        {
//            ReceivedRequest = request;
//            ContentHandler = contentHandler;
//            BestAttemptContent = (UNMutableNotificationContent)request.Content.MutableCopy();

//            (OneSignal.Current as OneSignalImplementation).DidReceiveNotificationExtensionRequest(request, BestAttemptContent);

//            ContentHandler(BestAttemptContent);
//        }

//        public override void TimeWillExpire()
//        {
//            // Called just before the extension will be terminated by the system.
//            // Use this as an opportunity to deliver your "best attempt" at modified content, otherwise the original push payload will be used.

//            (OneSignal.Current as OneSignalImplementation).ServiceExtensionTimeWillExpireRequest(ReceivedRequest, BestAttemptContent);

//            ContentHandler(BestAttemptContent);
//        }
//    }
//}