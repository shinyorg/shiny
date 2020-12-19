using System;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.Settings
{
    public class AppSettings : ReactiveObject, IAppSettings
    {
        const string DEFAULT_TRANSFER = "http://ipv4.download.thinkbroadband.com/1GB.zip";


        public AppSettings()
        {
            this.WhenAnyValue(x => x.LastTransferUrl)
                .Where(x => x == null)
                .Subscribe(_ => this.LastTransferUrl = DEFAULT_TRANSFER);

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
        [Reactive] public string LastTransferUrl { get; set; } = DEFAULT_TRANSFER;
    }
}
