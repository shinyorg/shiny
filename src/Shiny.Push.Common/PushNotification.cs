using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Shiny.Notifications;


namespace Shiny.Push
{
    public struct PushNotification
    {
        public PushNotification(IDictionary<string, string> data, Notification? notification)
        {
            this.Data = new ReadOnlyDictionary<string, string>(data);
            this.Notification = notification;
        }


        public ReadOnlyDictionary<string, string> Data { get; }
        public Notification? Notification { get; }
    }
}
