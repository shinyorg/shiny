using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny.Notifications;


namespace Samples.Notifications
{
    public class BadgeViewModel : ViewModel
    {
        readonly INotificationManager notifications;


        public BadgeViewModel(INotificationManager notifications)
        {
            this.notifications = notifications;

            this.WhenAnyValue(x => x.Badge)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .DistinctUntilChanged()
                .Subscribe(badge => notifications.Badge = badge)
                .DisposeWith(this.DeactivateWith);

            this.Clear = ReactiveCommand.Create(() => this.Badge = 0);
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.Badge = this.notifications.Badge;
        }


        public ICommand Clear { get; }
        [Reactive] public int Badge { get; set; }
    }
}

