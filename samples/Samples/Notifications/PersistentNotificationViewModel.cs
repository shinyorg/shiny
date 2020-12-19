using System;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;
using Shiny.Notifications;


namespace Samples.Notifications
{
    public class PersistentNotificationViewModel : ViewModel
    {
        IPersistentNotification notification;
        
        public PersistentNotificationViewModel(INotificationManager notifications)
        {
            var ext = notifications as IPersistentNotificationManagerExtension;
            this.IsSupported = ext != null;

            this.WhenAnyValue(x => x.IsIndeterministic)
                .Skip(1)
                .Subscribe(x => this.notification?.SetIndeterministicProgress(x));

            this.WhenAnyValue(x => x.Progress)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(x => this.notification?.SetProgress(
                    ToValue(x),
                    ToValue(this.Total)
                ));

            this.WhenAnyValue(x => x.Total)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(x => this.notification?.SetProgress(
                    ToValue(this.Progress),
                    ToValue(x)
                ));

            this.Toggle = ReactiveCommand.Create(
                () =>
                {
                    if (this.notification != null)
                    {
                        this.notification?.Dispose();
                        this.notification = null;
                    }
                    else
                    {
                        this.notification = ext.Create(new Notification
                        {
                            Title = "Test",
                            Message = "Test"
                        });
                        if (this.IsIndeterministic)
                        {
                            this.notification.SetIndeterministicProgress(true);
                        }
                        else
                        {
                            this.notification.SetProgress(
                                ToValue(this.Progress),
                                ToValue(this.Total)
                            );
                        }
                    }
                }
            );
        }


        public ICommand Toggle { get; }
        public bool IsSupported { get; }
        [Reactive] public string NotificationTitle { get; set; } = "Test";
        [Reactive] public string Description { get; set; } = "Description";
        [Reactive] public bool IsIndeterministic { get; set; }
        [Reactive] public double Total { get; set; } = 1.0;
        [Reactive] public double Progress { get; set; } = 0.0;

        static int ToValue(double value) => Convert.ToInt32(value * 100);
    }
}
