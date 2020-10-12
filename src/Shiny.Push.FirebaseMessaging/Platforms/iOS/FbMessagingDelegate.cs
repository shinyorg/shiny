using System;
using Foundation;
using Firebase.CloudMessaging;


namespace Shiny.Push.FirebaseMessaging
{
    public class FbMessagingDelegate : NSObject, IMessagingDelegate
    {
        readonly Action<RemoteMessage> onMessage;
        readonly Action<string> onToken;


        public FbMessagingDelegate(Action<RemoteMessage> onMessage, Action<string> onToken)
        {
            this.onMessage = onMessage;
            this.onToken = onToken;
        }


        public void DidReceiveMessage(Messaging messaging, RemoteMessage message)
            => this.onMessage(message);


        public void DidReceiveRegistrationToken(Messaging messaging, string fcmToken)
            => this.onToken(fcmToken);
    }
}
