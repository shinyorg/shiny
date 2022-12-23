using Shiny;
using Shiny.Notifications;

namespace Sample.Notifications;


public class OtherViewModel : ViewModel
{
    readonly INotificationManager notifications;
    IDisposable? sub;


    public OtherViewModel(BaseServices services, INotificationManager notifications) : base(services)
    {
        this.notifications = notifications;
        this.ClearBadge = new Command(() => this.Badge = 0);

        this.PermissionCheck = new Command(async () =>
        {
            var result = await this.notifications.RequestAccess(AccessRequestFlags.All);
            await this.Dialogs.DisplayAlertAsync("Permission", "Permission Check Result: " + result, "OK");
        });

        this.QuickSend = this.LoadingCommand(async () =>
            await this.notifications.Send("QUICK SEND TITLE", "This is a quick message")
        );

        this.StartChat = this.LoadingCommand(async () => 
        {
            this.notifications.RemoveChannel("ChatName");
            this.notifications.RemoveChannel("ChatAnswer");

            this.notifications.AddChannel(new Channel
            {
                Identifier = "ChatName",
                Importance = ChannelImportance.Normal,
                Actions =
                {
                    new ChannelAction
                    {
                        Identifier = "name",
                        Title = "What is your name?",
                        ActionType = ChannelActionType.TextReply
                    }
                }

            });
            this.notifications.AddChannel(new Channel
            {
                Identifier = "ChatAnswer",
                Actions =
                {
                    new ChannelAction
                    {
                        Title = "Yes",
                        Identifier = "yes",
                        ActionType = ChannelActionType.None
                    },
                    new ChannelAction
                    {
                        Title = "No",
                        Identifier = "no",
                        ActionType = ChannelActionType.Destructive
                    }
                }
            });

            await this.notifications.Send(
                "Shiny Chat",
                "Hi, What's your name?",
                "ChatName",
                DateTime.Now.AddSeconds(10)
            );
        });
    }


    public override async Task InitializeAsync(INavigationParameters parameters)
    {
        this.Badge = (await this.notifications.TryGetBadge()).Value ?? 0;

        this.sub = this.WhenAnyProperty(x => x.Badge)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Select(x => Observable.FromAsync(() => this.notifications.TrySetBadge(x)))
            .Switch()
            .Subscribe()
            .DisposedBy(this.DestroyWith);

        await base.InitializeAsync(parameters);
    }


    public ICommand QuickSend { get; }
    public ICommand StartChat { get; }
    public ICommand PermissionCheck { get; }
    public ICommand ClearBadge { get; }
    [Reactive] public int Badge { get; set; }
}

