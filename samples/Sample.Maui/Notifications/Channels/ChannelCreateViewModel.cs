using Shiny;
using Shiny.Notifications;

namespace Sample.Notifications.Channels;


public class ChannelCreateViewModel : ViewModel
{
    public ChannelCreateViewModel(BaseServices services, INotificationManager manager) : base(services)
    {
        this.Action1 = new ActionViewModel(this.Services);
        this.Action2 = new ActionViewModel(this.Services);

        this.Create = ReactiveCommand.CreateFromTask(async () =>
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
            manager.AddChannel(this.ToChannel());
            await this.Navigation.GoBack();
        });
    }


    public ICommand Create { get; }
    public ChannelImportance[] Importances => new[]
    {
        ChannelImportance.Critical,
        ChannelImportance.High,
        ChannelImportance.Normal,
        ChannelImportance.Low
    };
    [Reactive] public ChannelImportance SelectedImportance { get; set; } = ChannelImportance.Normal;

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
            // TODO
            //if (this.UseEmbeddedSound)
            //    channel.SetSoundFromEmbeddedResource(this.GetType().Assembly, "Sample.Resource.notification.mp3");
            //else
            //    channel.CustomSoundPath = "notification.mp3";

            channel.Sound = ChannelSound.Custom;
        }

        if (this.Action1.IsEnabled)
            channel.Actions.Add(this.Action1.ToAction());

        if (this.Action2.IsEnabled)
            channel.Actions.Add(this.Action2.ToAction());

        return channel;
    }
}