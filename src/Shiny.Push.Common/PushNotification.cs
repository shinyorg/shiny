using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Shiny.Push
{
    public struct PushNotification
    {
        public PushNotification(IDictionary<string, string> data, Notification? notification)
        { 
            this.Data = new ReadOnlyDictionary<string, string>(data);
            this.Notification = notification;
        }


        /// <summary>
        /// Data payload received - if not sent from your server side, background will not fire
        /// </summary>
        public IReadOnlyDictionary<string, string> Data { get; }

        /// <summary>
        /// Notification properties received - this will be null if received in the background
        /// </summary>
        public Notification? Notification { get; }
    }
}
