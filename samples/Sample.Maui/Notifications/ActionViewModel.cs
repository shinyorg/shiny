using System;
using System.Windows.Input;
using Shiny.Notifications;
using Xamarin.Forms;


namespace Sample
{
    public class ActionViewModel : SampleViewModel
    {
        public ActionViewModel()
        {
            this.SelectType = new Command(async () =>
            {
                this.type = await this.Choose(
                    "Choose Action Type",
                    ChannelActionType.None.ToString(),
                    ChannelActionType.OpenApp.ToString(),
                    ChannelActionType.TextReply.ToString(),
                    ChannelActionType.Destructive.ToString()
                );
            });
        }


        public ICommand SelectType { get; }


        bool enabled;
        public bool IsEnabled
        {
            get => this.enabled;
            set => this.Set(ref this.enabled, value);
        }


        string id;
        public string Identifier
        {
            get => this.id;
            set => this.Set(ref this.id, value);
        }


        string title;
        public string Title
        {
            get => this.title;
            set => this.Set(ref this.title, value);
        }


        string type = ChannelActionType.None.ToString();
        public string Type
        {
            get => this.type;
            private set => this.Set(ref this.type, value);
        }


        public ChannelAction ToAction() => new ChannelAction
        {
            Identifier = this.Identifier,
            Title = this.Title,
            ActionType = (ChannelActionType) Enum.Parse(typeof(ChannelActionType), this.Type)
        };
    }
}
