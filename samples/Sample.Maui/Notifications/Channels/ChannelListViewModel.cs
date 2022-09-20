using Shiny;
using Shiny.Notifications;

namespace Sample.Notifications.Channels;


public class ChannelListViewModel : ViewModel
{
    public ChannelListViewModel(BaseServices services, INotificationManager notifications) : base(services)
    {
        this.Create = this.Navigation.Command("NotificationsChannelCreate");

        this.LoadChannels = ReactiveCommand.CreateFromTask(async () =>
        {
            var channels = await notifications.GetChannels();
            this.Channels = channels
                .Select(x => new CommandItem
                {
                    Text = x.Identifier,
                    PrimaryCommand = this.ConfirmCommand(
                        "Are you sure you wish to delete this channel?",
                        async () =>
                        {
                            await notifications.RemoveChannel(x.Identifier);
                            this.LoadChannels.Execute(null);
                        }
                    )
                })
                .ToList();
        });
    }


    public ICommand Create { get; }
    public ICommand LoadChannels { get; }
    [Reactive] public IList<CommandItem> Channels { get; private set; }

    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.LoadChannels.Execute(null);
        return base.InitializeAsync(parameters);
    }
}
