using System;
using System.Reactive.Linq;
using Shiny;
using Shiny.Locations;


namespace Sample
{
    public class OtherExtensionsViewModel : SampleViewModel
    {
        readonly IMotionActivityManager activityManager = ShinyHost.Resolve<IMotionActivityManager>();
        IDisposable? sub;


        public override void OnAppearing()
        {
            base.OnAppearing();

            this.sub = Observable
                .Interval(TimeSpan.FromSeconds(5))
                .SubOnMainThread(async _ =>
                {
                    try
                    {
                        var current = await this.activityManager.GetCurrentActivity();
                        if (current == null)
                        {
                            this.CurrentText = "No current activity found - this should not happen";
                            this.IsCurrentAuto = false;
                            this.IsCurrentStationary = false;
                        }
                        else
                        {
                            this.CurrentText = $"{current.Types} ({current.Confidence})";

                            this.IsCurrentAuto = await this.activityManager.IsCurrentAutomotive();
                            this.IsCurrentStationary = await this.activityManager.IsCurrentStationary();
                        }
                    }
                    catch (Exception ex)
                    {
                        await this.Alert(ex.ToString());
                    }
                });
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.sub?.Dispose();
        }


        string text;
        public string CurrentText
        {
            get => this.text;
            private set => this.Set(ref this.text, value);
        }


        bool auto;
        public bool IsCurrentAuto
        {
            get => this.auto;
            private set => this.Set(ref this.auto, value);
        }


        bool stationary;
        public bool IsCurrentStationary
        {
            get => this.stationary;
            private set => this.Set(ref this.stationary, value);
        }
    }
}
