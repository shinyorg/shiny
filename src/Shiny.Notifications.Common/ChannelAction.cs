namespace Shiny.Notifications;

public class ChannelAction
{
    public string Identifier { get; set; }
    public string Title { get; set; }
    public ChannelActionType ActionType { get; set; } = ChannelActionType.None;


    public static ChannelAction Create(string id, ChannelActionType actionType = ChannelActionType.None) => new ChannelAction
    {
        Identifier = id,
        Title = id,
        ActionType = actionType
    };


    public static ChannelAction Create(string id, string title, ChannelActionType actionType = ChannelActionType.None) => new ChannelAction
    {
        Identifier = id,
        Title = title,
        ActionType = actionType
    };
}


public enum ChannelActionType
{
    TextReply,
    Destructive,
    OpenApp,
    None
    //AuthRequired
}
