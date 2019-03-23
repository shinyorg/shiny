using System;
using ReactiveUI.Fody.Helpers;
using Shiny;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using ReactiveUI;

namespace Samples.Settings
{
    public class BasicViewModel : ViewModel
    {
        public BasicViewModel(AppSettings appSettings)
        {
            this.IsChecked = appSettings.IsChecked;
            this.YourText = appSettings.YourText;

            appSettings
                .WhenAnyValue(x => x.LastUpdated)
                .Subscribe(x => this.LastUpdated = x)
                .DisposeWith(this.DestroyWith);

            this.WhenAnyValue(
                x => x.IsChecked,
                x => x.YourText
            )
            .Subscribe(_ =>
            {
                appSettings.IsChecked = this.IsChecked;
                appSettings.YourText = this.YourText;
            })
            .DisposeWith(this.DeactivateWith);
        }


        [Reactive] public bool IsChecked { get; set; }
        [Reactive] public string YourText { get; set; }
        [Reactive] public DateTime? LastUpdated { get; private set; }
    }
}
