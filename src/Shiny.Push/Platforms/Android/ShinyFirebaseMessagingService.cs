using System;
using Android.App;
using Firebase.Messaging;
using Shiny.Settings;

namespace Shiny.Push
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class ShinyFirebaseService : FirebaseMessagingService
    {
        public override async void OnMessageReceived(RemoteMessage message)
        {
            await ShinyHost.Container.SafeResolveAndExecute<IPushDelegate>(async ndelegate =>
            {
                var push = new PushNotification(message);
                await ndelegate.OnReceived(push);
                ShinyHost
                    .Resolve<IMessageBus>()
                    .Publish(push);
            });
        }


        public override async void OnNewToken(string token)
        {
            var settings = ShinyHost.Resolve<ISettings>();
            settings.SetRegToken(token);
            settings.SetRegDate(DateTime.UtcNow);

            await ShinyHost
                .Container
                .SafeResolveAndExecute<IPushDelegate>(x => x.OnTokenChanged(token));
        }
    }
}
