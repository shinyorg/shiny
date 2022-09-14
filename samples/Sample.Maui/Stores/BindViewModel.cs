using System;
using Shiny;
using Shiny.Stores;


namespace Sample
{
    public class BindViewModel : SampleViewModel
    {
        readonly IObjectStoreBinder binder = ShinyHost.Resolve<IObjectStoreBinder>();
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
            this.binder.Bind(this, "settings");
            this.sub = this.WhenAnyProperty()
                .Subscribe(_ => this.LastUpdated = DateTime.Now);
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.binder.UnBind(this);
            this.sub?.Dispose();
        }
    }
}
