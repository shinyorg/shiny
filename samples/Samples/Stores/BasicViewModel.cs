using System;
using ReactiveUI.Fody.Helpers;
using Shiny;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using ReactiveUI;
using Shiny.Stores;
using System.Windows.Input;
using Samples.Infrastructure;


namespace Samples.Stores
{
    public class BasicViewModel : ViewModel
    {
        readonly IAppSettings appSettings;


        public BasicViewModel(IAppSettings appSettings)
        {
            this.appSettings = appSettings;
        }


        [Reactive] public bool IsChecked { get; set; }
        [Reactive] public string YourText { get; set; }
        [Reactive] public DateTime? LastUpdated { get; private set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.IsChecked = this.appSettings.IsChecked;
            this.YourText = this.appSettings.YourText;
            this.LastUpdated = this.appSettings.LastUpdated;

            this.appSettings
                .WhenAnyValue(x => x.LastUpdated)
                .Subscribe(x => this.LastUpdated = x)
                .DisposeWith(this.DeactivateWith);

            this.WhenAnyValue(
                x => x.IsChecked,
                x => x.YourText
            )
            .Subscribe(_ =>
            {
                this.appSettings.IsChecked = this.IsChecked;
                this.appSettings.YourText = this.YourText;
            })
            .DisposeWith(this.DeactivateWith);
        }
    }
}
