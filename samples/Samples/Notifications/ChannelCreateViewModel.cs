using System;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.Notifications;


namespace Samples.Notifications
{
    public class ChannelCreateViewModel : ViewModel
    {
        public ChannelCreateViewModel(INavigationService navigator,
                                      INotificationManager manager,
                                      IDialogs dialogs)
        {
            this.Create = ReactiveCommand.CreateFromTask
            (
                async () =>
                {
                    await manager.CreateChannel(this.ToChannel());
                    await navigator.GoBack();
                },
                this.WhenAny(
                    x => x.Identifier,
                    x => x.Description,
                    (id, desc) =>
                        !id.GetValue().IsEmpty() &&
                        !desc.GetValue().IsEmpty()
                )
            );

            this.PickImportance = dialogs.PickEnumValueCommand<ChannelImportance>(
                "Importance",
                x => this.Importance = x.ToString()
            );

            this.PickActionType1 = dialogs.PickEnumValueCommand<ChannelActionType>(
                "Action Type",
                x => this.Action1ActionType = x.ToString(),
                this.WhenAny(
                    x => x.UseAction1,
                    x => x.GetValue()
                )
            );
            this.PickActionType2 = dialogs.PickEnumValueCommand<ChannelActionType>(
                "Action Type",
                x => this.Action1ActionType = x.ToString(),
                this.WhenAny(
                    x => x.UseAction2,
                    x => x.GetValue()
                )
            );
        }


        public ICommand Create { get; }
        public ICommand PickActionType1 { get; }
        public ICommand PickActionType2 { get; }
        public ICommand PickImportance { get; }

        [Reactive] public string Identifier { get; set; }
        [Reactive] public string Description { get; set; }
        [Reactive] public string Importance { get; private set; } = ChannelImportance.Normal.ToString();
        [Reactive] public string Sound { get; set; }

        [Reactive] public bool UseAction1 { get; set; }
        [Reactive] public string Action1Identifier { get; set; }
        [Reactive] public string Action1Title { get; set; }
        [Reactive] public string Action1ActionType { get; private set; } = ChannelActionType.None.ToString();

        [Reactive] public bool UseAction2 { get; set; }
        [Reactive] public string Action2Identifier { get; set; }
        [Reactive] public string Action2Title { get; set; }
        [Reactive] public string Action2ActionType { get; private set; } = ChannelActionType.None.ToString();

        //NotificationSound GetSound()
        //{
        //    switch (this.SelectedSoundType)
        //    {
        //        case "Default"  : return NotificationSound.Default;
        //        //case "Priority" : return NotificationSound.Default;
        //        case "Custom"   : return NotificationSound.FromCustom("notification.mp3");
        //        default         : return NotificationSound.None;
        //    }
        //}

        //public string[] SoundTypes { get; } = new[]
        //   {
        //    "None",
        //    "Default",
        //    "Custom"
        //    //"Priority"
        //};
        //[Reactive] public string SelectedSoundType { get; set; } = "None";

        Channel ToChannel()
        {
            var channel = new Channel
            {
                Identifier = this.Identifier,
                Description = this.Description,
                Importance = (ChannelImportance)Enum.Parse(typeof(ChannelImportance), this.Importance)
            };

            if (this.UseAction1)
            {
                channel.Actions.Add(new ChannelAction
                {
                    Identifier = this.Action1Identifier,
                    Title = this.Action1Title,
                    ActionType = (ChannelActionType)Enum.Parse(typeof(ChannelActionType), this.Action1ActionType)
                });
            }
            if (this.UseAction2)
            {
                channel.Actions.Add(new ChannelAction
                {
                    Identifier = this.Action2Identifier,
                    Title = this.Action2Title,
                    ActionType = (ChannelActionType)Enum.Parse(typeof(ChannelActionType), this.Action2ActionType)
                });
            }

            return channel;
        }
    }
}