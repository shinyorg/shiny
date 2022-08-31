using System;
using Foundation;
using Firebase.CloudMessaging;

namespace Shiny.Push.FirebaseMessaging;


public class FbMessagingDelegate : NSObject, IMessagingDelegate
{
    readonly Action<string> onToken;


    public FbMessagingDelegate(Action<string> onToken)
    {
        this.onToken = onToken;
    }


    [Export("messaging:didReceiveRegistrationToken:")]
    public void DidReceiveRegistrationToken(Messaging messaging, string fcmToken)
        => this.onToken(fcmToken);
}
