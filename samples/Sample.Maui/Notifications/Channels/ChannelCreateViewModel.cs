using System;
using System.Windows.Input;
using Shiny;
using Shiny.Notifications;
using Xamarin.Forms;


namespace Sample.Channels
{
    public class ChannelCreateViewModel : SampleViewModel
    {
        public ChannelCreateViewModel()
        {
            var manager = ShinyHost.Resolve<INotificationManager>();

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
                await this.Navigation.PopAsync();
            });

            this.PickImportance = new Command(async () =>
            {
                this.Importance = await this.Choose(
                    "Importance",
                    ChannelImportance.Critical.ToString(),
                    ChannelImportance.High.ToString(),
                    ChannelImportance.Normal.ToString(),
                    ChannelImportance.Low.ToString()
                );
            });
        }


        public ICommand Create { get; }
        public ICommand PickImportance { get; }

        public ActionViewModel Action1 { get; } = new ActionViewModel();
        public ActionViewModel Action2 { get; } = new ActionViewModel();


        string id;
        public string Identifier
        {
            get => this.id;
            set => this.Set(ref this.id, value);
        }


        string desc;
        public string Description
        {
            get => this.desc;
            set => this.Set(ref this.desc, value);
        }


        string imp = ChannelImportance.Normal.ToString();
        public string Importance
        {
            get => this.imp;
            private set => this.Set(ref this.imp, value);
        }


        bool embedded;
        public bool UseEmbeddedSound
        {
            get => this.embedded;
            set => this.Set(ref this.embedded, value);
        }


        bool custom;
        public bool UseCustomSound
        {
            get => this.custom;
            set => this.Set(ref this.custom, value);
        }


        string sound;
        public string Sound
        {
            get => this.sound;
            set => this.Set(ref this.sound, value);
        }


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
}