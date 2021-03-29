using System;
using System.Collections.ObjectModel;
using Shiny.Notifications;


namespace Shiny.Push
{
    public struct PushNotificationResponse
    {
        public PushNotificationResponse(Notification notification, string? actionIdentifier, string? text)
        {
            this.Notification = notification;
            this.ActionIdentifier = actionIdentifier;
            this.Text = text;

            this.Data = new ReadOnlyDictionary<string, string>(notification.Payload);
        }


        public ReadOnlyDictionary<string, string> Data { get; }
        public Notification Notification { get; }
        public string? ActionIdentifier { get; }
        public string? Text { get; }
    }
}
