using Shiny;
using Shiny.Notifications;

namespace Sample.Notifications.Channels;


public class ChannelCreateViewModel : ViewModel
{
    public ChannelCreateViewModel(BaseServices services, INotificationManager manager) : base(services)
    {
        this.Action1 = new ActionViewModel(this.Services);
        this.Action2 = new ActionViewModel(this.Services);

        this.Create = new Command(async () =>
        {
            if (this.Identifier.IsEmpty())
            {
                await this.Alert("Identifier is required");
                return;
            }
            if (this.Description.IsEmpty())
            {
                await this.Alert("Description is required");
                return;
            }
            await manager.AddChannel(this.ToChannel());
            await this.Navigation.GoBack();
        });

        this.PickImportance = new Command(async () =>
        {
            //this.Importance = await this.Choose(
            //    "Importance",
            //    ChannelImportance.Critical.ToString(),
            //    ChannelImportance.High.ToString(),
            //    ChannelImportance.Normal.ToString(),
            //    ChannelImportance.Low.ToString()
            //);
        });
    }


    public ICommand Create { get; }
    public ICommand PickImportance { get; }

    public ActionViewModel Action1 { get; }
    public ActionViewModel Action2 { get; }


    [Reactive] public string Identifier { get; set; }
    [Reactive] public string Description { get; set; }
    [Reactive] public string Importance { get; private set; } = ChannelImportance.Normal.ToString();
    [Reactive] public bool UseEmbeddedSound { get; set; }
    [Reactive] public bool UseCustomSound { get; set; }
    [Reactive] public string Sound { get; set; }
    Channel ToChannel()
    {
        var channel = new Channel
        {
            Identifier = this.Identifier,
            Description = this.Description,
            Importance = (ChannelImportance)Enum.Parse(typeof(ChannelImportance), this.Importance)
        };

        if (this.UseCustomSound)
        {
            if (this.UseEmbeddedSound)
                channel.SetSoundFromEmbeddedResource(this.GetType().Assembly, "Sample.Resource.notification.mp3");
            else
                channel.CustomSoundPath = "notification.mp3";

            channel.Sound = ChannelSound.Custom;
        }

        if (this.Action1.IsEnabled)
            channel.Actions.Add(this.Action1.ToAction());

        if (this.Action2.IsEnabled)
            channel.Actions.Add(this.Action2.ToAction());

        return channel;
    }
}