using System;


namespace Shiny.Notifications
{
    public class ChannelAction
    {
        public string Identifier { get; set; }
        public string Title { get; set; }
        public NotificationActionType ActionType { get; set; } = NotificationActionType.None;
    }


    public enum NotificationActionType
    {
        TextReply,
        Destructive,
        OpenApp,
        None
        //AuthRequired
    }
}
