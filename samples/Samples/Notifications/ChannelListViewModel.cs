using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using Samples.Infrastructure;
using Shiny.Notifications;


namespace Samples.Notifications
{
    public class ChannelListViewModel : ViewModel
    {
        public ChannelListViewModel(INavigationService navigator,
                                    INotificationManager notifications,
                                    IDialogs dialogs)
        {
            this.Create = navigator.NavigateCommand("NotificationChannelCreate");

            this.LoadChannels = ReactiveCommand.CreateFromTask(async () =>
            {
                var channels = await notifications.GetChannels();
                this.Channels = channels
                    .Select(x => new CommandItem
                    {
                        Text = x.Identifier,
                        SecondaryCommand = ReactiveCommand.CreateFromTask(async () =>
                        {
                            var confirm = await dialogs.Confirm("Are you sure you wish to delete this channel?");
                            if (confirm)
                            {
                                await notifications.DeleteChannel(x.Identifier);
                                this.LoadChannels.Execute(null);
                            }
                        })
                    })
                    .ToList();

                this.RaisePropertyChanged(nameof(this.Channels));
            });
        }


        public ICommand Create { get; }
        public ICommand LoadChannels { get; }
        public IList<CommandItem> Channels { get; private set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.LoadChannels.Execute(null);
        }
    }
}
