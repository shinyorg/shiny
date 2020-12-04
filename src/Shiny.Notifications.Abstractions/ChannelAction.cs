using System;


namespace Shiny.Notifications
{
    public class ChannelAction
    {
        public ChannelAction(string identifier, string title, NotificationActionType actionType = NotificationActionType.None)
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
