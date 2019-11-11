using System;
using System.Collections.Generic;


namespace Shiny.Notifications
{
    public class NotificationCategory
    {
        public NotificationCategory(string identifier) => this.Identifier = identifier;
        public string Identifier { get; }

        public List<NotificationAction> Actions { get; set; } = new List<NotificationAction>();
    }


    public class NotificationAction
    {
        public NotificationAction(string identifier, string title)
        {
            this.Identifier = identifier;
            this.Title = title;
        }


        public string Identifier { get; }
        public string Title { get;  }
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
