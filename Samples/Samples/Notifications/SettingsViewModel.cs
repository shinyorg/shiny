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
    public class SettingsViewModel : ViewModel, IAppSettings
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
                    this.GetType()
                        .GetProperties()
                        .Where(y =>
                            y.Name.StartsWith("UsesNotifications") &&
                            y.CanWrite &&
                            y.PropertyType == typeof(bool)
                        )
                        .ToList()
                        .ForEach(y => y.SetValue(this, x))
                )
                .DisposeWith(this.DeactivateWith);

            this.WhenAnyProperty()
                .Skip(1)
                .Where(x => x.Value != "ToggleAll")
                .Subscribe(x => appSettings.SetValue(x.Value, this.GetValue(x.Value)))
                .DisposeWith(this.DeactivateWith);
        }


        [Reactive] public bool ToggleAll { get; set; }
        [Reactive] public bool UseNotificationsBle { get; set; }
        [Reactive] public bool UseNotificationsHttpTransfers { get; set; }
        [Reactive] public bool UseNotificationsBeaconRegionEntry { get; set; }
        [Reactive] public bool UseNotificationsBeaconRegionExit { get; set; }
        [Reactive] public bool UseNotificationsGeofenceEntry { get; set; }
        [Reactive] public bool UseNotificationsGeofenceExit { get; set; }
        [Reactive] public bool UseNotificationsJobStart { get; set; }
        [Reactive] public bool UseNotificationsJobFinish { get; set; }
    }
}