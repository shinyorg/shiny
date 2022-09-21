using Shiny.Notifications;

namespace Sample.Notifications.Channels;


public class ActionViewModel : ViewModel
{
    public ActionViewModel(BaseServices services) : base(services) {}


    public ChannelActionType[] Types => new[]
    {
        ChannelActionType.None,
        ChannelActionType.OpenApp,
        ChannelActionType.TextReply,
        ChannelActionType.Destructive
    };

    [Reactive] public ChannelActionType SelectedType { get; set; } = ChannelActionType.None;
    [Reactive] public bool IsEnabled { get; set; }
    [Reactive] public string Identifier { get; set; }
    [Reactive] public string Title { get; set; }
    public ChannelAction ToAction() => new ChannelAction
    {
        Identifier = this.Identifier,
        Title = this.Title,
        ActionType = this.SelectedType
    };
}
