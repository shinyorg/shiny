using System;
using System.Collections.Generic;


namespace Shiny.Notifications
{
    public class NotificationCategory
    {
        public string Identifier { get; set; }
        public List<NotificationAction> Actions { get; set; } = new List<NotificationAction>();
    }


    public class NotificationAction
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
