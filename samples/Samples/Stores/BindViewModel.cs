using System;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny.Stores;


namespace Samples.Stores
{
    public class BindViewModel : ViewModel
    {
        readonly IObjectStoreBinder binder;


        public BindViewModel(IObjectStoreBinder binder)
        {
            this.binder = binder;
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
            this.binder.Bind(this, "settings");
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.binder.UnBind(this);
        }
    }
}
