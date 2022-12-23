using Shiny;
using Shiny.Notifications;

namespace Sample.Notifications.Channels;


public class ChannelListViewModel : ViewModel
{
    public ChannelListViewModel(BaseServices services, INotificationManager notifications) : base(services)
    {
        this.Create = this.Navigation.Command("NotificationsChannelCreate");

        this.LoadChannels = ReactiveCommand.Create(() =>
            this.Channels = notifications
                .GetChannels()
                .Select(x => new CommandItem
                {
                    Text = x.Identifier,
                    PrimaryCommand = this.ConfirmCommand(
                        "Are you sure you wish to delete this channel?",
                        async () =>
                        {
                            notifications.RemoveChannel(x.Identifier);
                            this.LoadChannels!.Execute(null);
                        }
                    )
                })
                .ToList()
        );
    }


    public ICommand Create { get; }
    public ICommand LoadChannels { get; }
    [Reactive] public IList<CommandItem> Channels { get; private set; }

    public override void OnNavigatedTo(INavigationParameters parameters) => this.LoadChannels.Execute(null);
}
