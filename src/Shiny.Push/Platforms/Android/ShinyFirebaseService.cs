using System;
using System.Linq;
using Android.App;
using Firebase.Messaging;
using Shiny.Logging;
using Shiny.Notifications;
using Notification = Shiny.Notifications.Notification;


namespace Shiny.Push
{
    [Service]
    [IntentFilter(new[] { IntentAction })]
    public class ShinyFirebaseService : FirebaseMessagingService
    {
        public const string IntentAction = "com.google.firebase.MESSAGING_EVENT";
        readonly Lazy<INotificationManager> notifications = ShinyHost.LazyResolve<INotificationManager>();
        readonly Lazy<IMessageBus> msgBus = ShinyHost.LazyResolve<IMessageBus>();


        public override async void OnMessageReceived(RemoteMessage message) => await Log.SafeExecute(async () =>
        {
            await ShinyHost.Container.RunDelegates<IPushDelegate>(x => x.OnReceived(message.Data));
            this.msgBus.Value.Publish(
                nameof(ShinyFirebaseService),
                message.Data
            );

            var native = message.GetNotification();
            if (native != null)
            {
                var notification = new Notification
                {
                    Title = native.Title,
                    Message = native.Body,
                    Channel = native.ClickAction,

                    // recast this as implementation types aren't serializing well
                    Payload = message.Data?.ToDictionary(
                        x => x.Key,
                        x => x.Value
                    )
                };
                //if (!native.ChannelId.IsEmpty())
                //    notification.Android.ChannelId = native.ChannelId;

                if (!native.Icon.IsEmpty())
                    notification.Android.SmallIconResourceName = native.Icon;

                if (!native.Color.IsEmpty())
                    notification.Android.ColorResourceName = native.Color;

                // TODO: I have to intercept the response for the IPushDelegate.OnEntry
                await this.notifications.Value.Send(notification);
            }
        });


        public override async void OnNewToken(string token) => await Log.SafeExecute(async () =>
        {
            await ShinyHost.Container.RunDelegates<IPushDelegate>(x => x.OnTokenChanged(token));
            await ((IAndroidTokenUpdate)ShinyHost.Resolve<IPushManager>()).UpdateNativePushToken(token);
        });
    }
}
