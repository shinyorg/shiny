using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples
{
    public class DelegateNotificationsViewModel : ViewModel
    {
        public DelegateNotificationsViewModel(AppNotifications notifications)
        {
            this.Notifications = notifications
                .GetRegistrations()
                .Select(x => new DelegateNotificationItemViewModel(notifications, x))
                .ToList();

            this.WhenAnyValue(x => x.ToggleAll)
                .Skip(1)
                .Subscribe(x => this.Notifications.ForEach(y =>
                {
                    y.IsEntryEnabled = x;
                    y.IsExitEnabled = x;
                }))
                .DisposeWith(this.DeactivateWith);
        }


        public List<DelegateNotificationItemViewModel> Notifications { get; }
        [Reactive] public bool ToggleAll { get; set; }
    }


    public class DelegateNotificationItemViewModel : ReactiveObject
    {
        public DelegateNotificationItemViewModel(AppNotifications notifications, NotificationRegistration reg)
        {
            this.Description = reg.Description;
            this.Text = reg.HasEntryExit ? "Entry" : "Enabled";
            this.HasEntryExit = reg.HasEntryExit;

            this.IsEntryEnabled = notifications.IsEnabled(reg.Type, true);
            this.IsExitEnabled = notifications.IsEnabled(reg.Type, false);

            this.WhenAnyValue(x => x.IsEntryEnabled)
                .Skip(1)
                .Subscribe(x => notifications.Set(reg.Type, true, x));

            this.WhenAnyValue(x => x.IsExitEnabled)
                .Skip(1)
                .Subscribe(x => notifications.Set(reg.Type, false, x));
        }


        public string Description { get; }
        public string Text { get; }
        public bool HasEntryExit { get; }
        [Reactive] public bool IsEntryEnabled { get; set; }
        [Reactive] public bool IsExitEnabled { get; set; }
    }
}