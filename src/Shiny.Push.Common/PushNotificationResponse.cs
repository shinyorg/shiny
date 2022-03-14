using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Shiny.Push
{
    public struct PushNotificationResponse
    {
        public PushNotificationResponse( string? actionIdentifier, string? text)
        {
            this.ActionIdentifier = actionIdentifier;
            this.Text = text;

            this.Data = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        }


        public ReadOnlyDictionary<string, string> Data { get; }
        //public Notification Notification { get; }
        public string? ActionIdentifier { get; }
        public string? Text { get; }
    }
}
