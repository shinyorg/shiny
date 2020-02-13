using System;
using System.Collections.Generic;
using Firebase.Messaging;


namespace Shiny.Push
{
    class PushNotification : IPushNotification
    {
        readonly RemoteMessage native;
        readonly RemoteMessage.Notification notNative;

        public PushNotification(RemoteMessage native)
        {
            this.Data = new Dictionary<string, object>();
            this.native = native;
            this.notNative = this.native.GetNotification();
        }


        public string Title => this.notNative.Title;
        public string Body => this.notNative.Body;
        public int Badge => 0;
        public bool IsContentAvailable => false;
        public string Category => null;

        public IDictionary<string, object> Data { get; }
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
    }
}
