using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Settings;
using Shiny;


namespace Samples.Notifications
{
    public class SettingsViewModel : ViewModel
    {
        public SettingsViewModel(AppSettings appSettings)
        {
            appSettings
                .GetType()
                .GetProperties()
                .Where(x => x.CanRead && x.CanWrite && x.Name.StartsWith("UseNotifications"))
                .ToList()
                .ForEach(x =>
                    this.SetValue(x.Name, appSettings.GetValue(x.Name))
                );

            this.WhenAnyValue(x => x.ToggleAll)
                .Skip(1)
                .Subscribe(x =>
                {
                    this.UseNotificationsBle = x;
                    this.UseNotificationsGeofences = x;
                    this.UseNotificationsJobs = x;
                    this.UseNotificationsHttpTransfers = x;
                    this.UseNotificationsBeaconMonitoring = x;
                })
                .DisposeWith(this.DeactivateWith);

            this.WhenAnyProperty()
                .Skip(1)
                .Where(x => x.Value != "ToggleAll")
                .Subscribe(x => appSettings.SetValue(x.Value, this.GetValue(x.Value)))
                .DisposeWith(this.DeactivateWith);
        }

        [Reactive] public bool ToggleAll { get; set; }
        [Reactive] public bool UseNotificationsBle { get; set; }
        [Reactive] public bool UseNotificationsGeofences { get; set; }
        [Reactive] public bool UseNotificationsJobs { get; set; }
        [Reactive] public bool UseNotificationsHttpTransfers { get; set; }
        [Reactive] public bool UseNotificationsBeaconMonitoring { get; set; }
    }
}