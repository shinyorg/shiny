using System;
using Foundation;
using Firebase.CloudMessaging;


namespace Shiny.Push.FirebaseMessaging
{
    public class FbMessagingDelegate : MessagingDelegate
    {
        readonly Action<string> onToken;

        public FbMessagingDelegate(Action<string> onToken)
            => this.onToken = onToken;

        public override void DidReceiveRegistrationToken(Messaging messaging, string fcmToken)
            => this.onToken(fcmToken);
    }
}
