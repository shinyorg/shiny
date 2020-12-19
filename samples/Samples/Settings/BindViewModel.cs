using System;
using System.Reactive.Linq;
using Shiny.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Prism.Navigation;


namespace Samples.Settings
{
    public class BindViewModel : ViewModel
    {
        readonly ISettings settings;


        public BindViewModel(ISettings settings)
        {
            this.settings = settings;
            this.WhenAnyValue(
                x => x.YourText,
                x => x.IsChecked
            )
            .Skip(1)
            .Subscribe(_ => this.LastUpdated = DateTime.Now);
        }


        [Reactive] public bool IsChecked { get; set; }
        [Reactive] public string YourText { get; set; }
        [Reactive] public DateTime? LastUpdated { get; set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.settings.Bind(this);
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.settings.UnBind(this);
        }
    }
}
