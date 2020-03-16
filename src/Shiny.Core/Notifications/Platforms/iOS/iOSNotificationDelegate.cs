using System;
using UserNotifications;


namespace Shiny.Notifications  
{
    public class iOSNotificationDelegate : UNUserNotificationCenterDelegate, IShinyStartupTask
    {
        public void Start() =>
            UNUserNotificationCenter
                .Current
                .Delegate = this;



        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
        }


        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
        }
    }
}
