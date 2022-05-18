using System;
using Android.App;
using Firebase.Messaging;

namespace Shiny.Push;


[Service(Exported = true)]
[IntentFilter(new[] { IntentAction })]
public class ShinyFirebaseService : FirebaseMessagingService
{
    public const string IntentAction = "com.google.firebase.MESSAGING_EVENT";

    internal static Action<RemoteMessage>? MessageReceived { get; set; }
    public override void OnMessageReceived(RemoteMessage message)
        => MessageReceived?.Invoke(message);

    internal static Action<string>? NewToken { get; set; }
    public override void OnNewToken(string token)
        => NewToken?.Invoke(token);
}
