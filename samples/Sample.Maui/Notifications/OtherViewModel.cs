using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Shiny;
using Shiny.Notifications;
using Xamarin.Forms;


namespace Sample
{
    public class OtherViewModel : SampleViewModel
    {
        readonly INotificationManager notifications;
        IDisposable? sub;


        public OtherViewModel()
        {
            this.notifications = ShinyHost.Resolve<INotificationManager>();
            this.ClearBadge = new Command(() => this.Badge = 0);

            this.PermissionCheck = new Command(async () =>
            {
                var result = await this.notifications.RequestAccess(AccessRequestFlags.All);
                await this.Alert("Permission Check Result: " + result);
            });

            this.QuickSend = this.LoadingCommand(async () =>
                await this.notifications.Send("QUICK SEND TITLE", "This is a quick message")
            );

            this.StartChat = this.LoadingCommand(async () => 
            {
                await this.notifications.RemoveChannel("ChatName");
                await this.notifications.RemoveChannel("ChatAnswer");

                await this.notifications.AddChannel(new Channel
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
                await this.notifications.AddChannel(new Channel
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


        public override async void OnAppearing()
        {
            base.OnAppearing();
            this.Badge = await this.notifications.GetBadge();

            this.sub = this.WhenAnyProperty(x => x.Badge)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .DistinctUntilChanged()
                .Select(x => Observable.FromAsync(() => this.notifications.SetBadge(x)))
                .Switch()
                .Subscribe();
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.sub?.Dispose();
        }


        public ICommand QuickSend { get; }
        public ICommand StartChat { get; }
        public ICommand PermissionCheck { get; }
        public ICommand ClearBadge { get; }


        int badge;
        public int Badge
        {
            get => this.badge;
            set => this.Set(ref this.badge, value);
        }
    }
}

