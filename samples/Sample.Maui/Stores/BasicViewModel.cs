using Shiny;
using System;
using System.Reactive.Linq;


namespace Sample
{
    public class BasicViewModel : SampleViewModel
    {
        readonly IAppSettings appSettings = ShinyHost.Resolve<IAppSettings>();
        IDisposable? sub;


        bool isChecked;
        public bool IsChecked
        {
            get => this.isChecked;
            set => this.Set(ref this.isChecked, value);
        }

        string yourText;
        public string YourText
        {
            get => this.yourText;
            set => this.Set(ref this.yourText, value);
        }


        DateTime? lastUpdated;
        public DateTime? LastUpdated
        {
            get => this.lastUpdated;
            set => this.Set(ref this.lastUpdated, value);
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.sub = this.appSettings
                .WhenAnyProperty()
                .Subscribe(_ =>
                {
                    this.IsChecked = this.appSettings.IsChecked;
                    this.YourText = this.appSettings.YourText;
                    this.LastUpdated = this.appSettings.LastUpdated;
                });
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.sub?.Dispose();
        }
    }
}
