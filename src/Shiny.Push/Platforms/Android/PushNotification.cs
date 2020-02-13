using System;
using Firebase.Messaging;

namespace Shiny.Push
{
    class PushNotification : IPushNotification
    {
        readonly RemoteMessage native;
        readonly RemoteMessage.Notification notNative;

        public PushNotification(RemoteMessage native)
        { 
            this.native = native;
            this.notNative = this.native.GetNotification();
        }


        public string Title => this.notNative.Title;
        public string Body => this.notNative.Body;
    }
}
