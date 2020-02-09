using System;
using Foundation;
using Firebase.CloudMessaging;


namespace Shiny.Integrations.FirebaseNotifications
{
    public class FbMessagingDelegate : NSObject, IMessagingDelegate
    {
        public void DidReceiveMessage(Messaging messaging, RemoteMessage message)
        {

        }


        public void DidReceiveRegistrationToken(Messaging messaging, string fcmToken)
        {
            // TODO: update current token
            // TODO: fire shiny delegate
        }
    }
}
