using System;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.Notifications;


namespace Samples.Notifications
{
    public class PersistentNotificationViewModel : ViewModel
    {
        IPersistentNotification? notification;
        readonly IDialogs dialogs;


        public PersistentNotificationViewModel(INotificationManager notifications, IDialogs dialogs)
        {
            this.dialogs = dialogs;

            var ext = notifications as IPersistentNotificationManagerExtension;
            this.IsSupported = ext != null;
            this.Title = "Test";

            this.Sub(x => x.NotificationTitle, (value, n) => n.Title = value);
            this.Sub(x => x.NotificationMessage, (value, n) => n.Message = value);
            this.Sub(x => x.IsIndeterministic, (value, n) => n.IsIndeterministic = value);
            this.Sub(x => x.Progress, (value, n) => n.Progress = value);
            this.Sub(x => x.Total, (value, n) => n.Total = value);

            this.Toggle = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    if (this.notification == null)
                    {
                        this.notification = await ext!.Create(new Notification(), false);
                        this.notification.DisposedBy(this.DestroyWith);

                        this.notification.Title = this.NotificationTitle;
                        this.notification.Message = this.NotificationMessage;
                        this.notification.Total = this.Total;
                        this.notification.Progress = this.Progress;
                        this.notification.IsIndeterministic = this.IsIndeterministic;
                    }

                    if (this.notification.IsShowing)
                        this.notification.Dismiss();
                    else
                        this.notification.Show();
                }
            );
        }



        public ICommand Toggle { get; }
        public bool IsSupported { get; }
        [Reactive] public string NotificationTitle { get; set; } = "Title";
        [Reactive] public string NotificationMessage { get; set; } = "Description";
        [Reactive] public bool IsIndeterministic { get; set; }
        [Reactive] public double Total { get; set; } = 0;
        [Reactive] public double Progress { get; set; } = 0;


        void Sub<T>(Expression<Func<PersistentNotificationViewModel, T>> expression, Action<T, IPersistentNotification> action)
        {
            this.WhenAnyValue(expression)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(
                    x =>
                    {
                        if (this.notification != null)
                            action(x, this.notification);
                    },
                    ex => this.dialogs.Alert(ex.ToString())
                )
                .DisposedBy(this.DestroyWith);
        }
    }
}
