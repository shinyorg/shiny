using System;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.Settings
{
    public class AppSettings : ReactiveObject
    {
        public AppSettings()
        {
            this.WhenAnyValue(x => x.LastTransferUrl)
                .Where(x => x == null)
                .Subscribe(_ => this.LastTransferUrl = "http://ipv4.download.thinkbroadband.com/1GB.zip");

            this.WhenAnyValue(
                    x => x.IsChecked,
                    x => x.YourText
                )
                .Skip(1)
                .Subscribe(_ =>
                    this.LastUpdated = DateTime.Now
                );
        }


        [Reactive] public bool IsChecked { get; set; }
        [Reactive] public string YourText { get; set; }
        [Reactive] public DateTime? LastUpdated { get; set; }

        [Reactive] public string LastTransferUrl { get; set; } = "";
        [Reactive] public bool UseNotificationsBle { get; set; } = true;
        [Reactive] public bool UseNotificationsGeofences { get; set; } = true;
        [Reactive] public bool UseNotificationsJobs { get; set; } = true;
        [Reactive] public bool UseNotificationsHttpTransfers { get; set; } = true;
        [Reactive] public bool UseNotificationsBeaconMonitoring { get; set; } = true;
    }
}
