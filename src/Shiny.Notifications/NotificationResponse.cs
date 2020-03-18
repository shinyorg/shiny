using System;


namespace Shiny.Notifications
{
    public struct NotificationResponse
    {
        public NotificationResponse(Notification notification, string? actionIdentifier, string? text)
        {
            this.Notification = notification;
            this.ActionIdentifier = actionIdentifier;
            this.Text = text;
        }


        public Notification Notification { get; }
        public string? ActionIdentifier { get; }
        public string? Text { get; }
    }
}
