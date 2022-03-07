using System;
using System.Collections.ObjectModel;


namespace Shiny.Push
{
    public struct PushNotificationResponse
    {
        public PushNotificationResponse( string? actionIdentifier, string? text)
        {
            this.ActionIdentifier = actionIdentifier;
            this.Text = text;

         //   this.Data = new ReadOnlyDictionary<string, string>(notification.Payload);
        }


        public ReadOnlyDictionary<string, string> Data { get; }
        //public Notification Notification { get; }
        public string? ActionIdentifier { get; }
        public string? Text { get; }
    }
}
