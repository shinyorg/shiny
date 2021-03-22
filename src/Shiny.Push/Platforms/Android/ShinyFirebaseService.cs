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
        static readonly Subject<string> tokenSubj = new Subject<string>();
        static readonly Subject<IDictionary<string, string>> dataSubj = new Subject<IDictionary<string, string>>();
        readonly Lazy<INotificationManager> notifications = ShinyHost.LazyResolve<INotificationManager>();


        public override async void OnMessageReceived(RemoteMessage message)
        {
            dataSubj.OnNext(message.Data);
            await ShinyHost.Container.RunDelegates<IPushDelegate>(x => x.OnReceived(message.Data));

            var native = message.GetNotification();
            if (native != null)
            {
                var notification = new Notification
                {
                    Title = native.Title,
                    Message = native.Body,

                    // recast this as implementation types aren't serializing well
                    Payload = message.Data?.ToDictionary(
                        x => x.Key,
                        x => x.Value
                    )
                };
                if (!native.ChannelId.IsEmpty())
                    notification.Channel = native.ChannelId;

                if (!native.Icon.IsEmpty())
                    notification.Android.SmallIconResourceName = native.Icon;

                if (!native.Color.IsEmpty())
                    notification.Android.ColorResourceName = native.Color;

                // TODO: I have to intercept the response for the IPushDelegate.OnEntry
                await this.notifications.Value.Send(notification);
            }
        }


        public override async void OnNewToken(string token)
        {
            tokenSubj.OnNext(token);
            await ShinyHost.Container.RunDelegates<IPushDelegate>(
                x => x.OnTokenChanged(token)
            );
        }


        public static IObservable<string> WhenTokenChanged() => tokenSubj;


        public static IObservable<IDictionary<string, string>> WhenDataReceived() => dataSubj;
    }
}
