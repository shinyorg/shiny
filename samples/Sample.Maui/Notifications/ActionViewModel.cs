using Shiny.Notifications;

namespace Sample.Notifications;


public class ActionViewModel : ViewModel
{
    public ActionViewModel(BaseServices services) : base(services)
    {
        //this.SelectType = new Command(async () =>
        //{
        //    this.type = await this.Choose(
        //        "Choose Action Type",
        //        ChannelActionType.None.ToString(),
        //        ChannelActionType.OpenApp.ToString(),
        //        ChannelActionType.TextReply.ToString(),
        //        ChannelActionType.Destructive.ToString()
        //    );
        //});
    }


    public ICommand SelectType { get; }

    [Reactive] public bool IsEnabled { get; set; }
    [Reactive] public string Identifier { get; set; }
    [Reactive] public string Title { get; set; }
    [Reactive] public string Type { get; private set; } = ChannelActionType.None.ToString();
    public ChannelAction ToAction() => new ChannelAction
    {
        Identifier = this.Identifier,
        Title = this.Title,
        ActionType = (ChannelActionType) Enum.Parse(typeof(ChannelActionType), this.Type)
    };
}
