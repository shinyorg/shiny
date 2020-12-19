using System;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.Notifications;


namespace Samples.Notifications
{
    public class ChannelCreateViewModel : ViewModel
    {
        public ChannelCreateViewModel(INotificationManager manager, IDialogs dialogs)
        {
            this.Create = ReactiveCommand.CreateFromTask
            (
                () => manager.CreateChannel(this.ToChannel()),
                this.WhenAny(
                    x => x.Identifier,
                    x => !x.GetValue().IsEmpty()
                )
            );
        }


        public ICommand Create { get; }
        public ICommand PickActionType { get; }

        [Reactive] public string Identifier { get; set; }
        [Reactive] public string Description { get; set; }
        [Reactive] public string Sound { get; set; }
        [Reactive] public int Importance { get; set; }

        [Reactive] public bool UseAction1 { get; set; }
        [Reactive] public string Action1Identifier { get; set; }
        [Reactive] public string Action1Description { get; set; }
        [Reactive] public int Action1ActionType { get; set; }

        [Reactive] public bool UseAction2 { get; set; }
        [Reactive] public string Action2Identifier { get; set; }
        [Reactive] public string Action2Title { get; set; }
        [Reactive] public int Action2ActionType { get; set; }


        Channel ToChannel()
        {
            var channel = new Channel
            {
                Identifier = this.Identifier,
                Description = this.Description
                //Importance = ChannelImportance.Normal
            };

            return channel;
        }
    }
}