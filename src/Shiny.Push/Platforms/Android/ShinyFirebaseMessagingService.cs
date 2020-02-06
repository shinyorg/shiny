using System;
using Android.App;
using Firebase.Messaging;


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
                ndelegate.OnReceived("");
                //var notification = message.GetNotification();
                //notification.Body;
                //notification.BodyLocalizationKey
                //notification.ChannelId;
                //notification.ClickAction;
                //notification.Color
                //notification.Icon
                //notification.Link
                //notification.Sound
                //notification.Title;
                //notification.TitleLocalizationKey;
                //notification.Tag
                //message.SentTime
                //message.To
                //message.Priority
                //message.MessageId
                //message.MessageType
                //message.From
                //message.Data
            });
        }


        public override void OnNewToken(string token)
        {
            // token refreshes need to be stored
        }
    }
}
