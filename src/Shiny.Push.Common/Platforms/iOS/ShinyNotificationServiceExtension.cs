using System;
using Foundation;
using UserNotifications;


namespace Shiny.Notifications.Platforms.iOS
{
    public class ShinyNotificationServiceExtension : UNNotificationServiceExtension
    {
        protected ShinyNotificationServiceExtension(NSObjectFlag t) : base(t) {}


        public override void DidReceiveNotificationRequest(UNNotificationRequest request, Action<UNNotificationContent> contentHandler) 
        {
        }
    }
}
