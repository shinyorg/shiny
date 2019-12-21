using System;
using System.Collections.Generic;


namespace Shiny.Notifications
{
    public class NotificationCategory
    {
        public NotificationCategory(string identifier, params NotificationAction[] actions)
        {
            this.Identifier = identifier;
            this.Actions = new List<NotificationAction>();
            if (actions.Length > 0)
                this.Actions.AddRange(actions);
        }


        public string Identifier { get; }
        public List<NotificationAction> Actions { get; set; }
    }


    public class NotificationAction
    {
        public NotificationAction(string identifier, string title, NotificationActionType actionType = NotificationActionType.None)
        {
            this.Identifier = identifier;
            this.Title = title;
            this.ActionType = actionType;
        }


        public string Identifier { get; }
        public string Title { get;  }
        public NotificationActionType ActionType { get; }
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
