using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.App;
using Firebase.Messaging;
using Shiny.Notifications;
using Notification = Shiny.Notifications.Notification;


namespace Shiny.Push
{
    [Service]
    [IntentFilter(new[] { IntentAction })]
    public class ShinyFirebaseService : FirebaseMessagingService
    {
        public const string IntentAction = "com.google.firebase.MESSAGING_EVENT";
        
        public override void OnMessageReceived(RemoteMessage message)
            => msgSubj.OnNext(message);

        public override void OnNewToken(string token)
            => tokenSubj.OnNext(token);

        static readonly Subject<string> tokenSubj = new Subject<string>();
        public static IObservable<string> WhenTokenChanged() => tokenSubj;

        static readonly Subject<RemoteMessage> msgSubj = new Subject<RemoteMessage>();
        public static IObservable<RemoteMessage> WhenReceived()=> msgSubj;
    }
}
