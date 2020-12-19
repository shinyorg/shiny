using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny.Locations;


namespace Samples.MotionActivity
{
    public class OtherExtensionsViewModel : ViewModel
    {
        readonly IMotionActivityManager? activityManager;
        readonly IDialogs dialogs;


        public OtherExtensionsViewModel(IDialogs dialogs, IMotionActivityManager? activityManager = null)
        {
            this.dialogs = dialogs;
            this.activityManager = activityManager;
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            if (this.activityManager == null)
                return;

            Observable
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
                        await this.dialogs.Alert(ex.ToString());
                    }
                })
                .DisposeWith(this.DeactivateWith);
        }


        [Reactive] public string CurrentText { get; private set; }
        [Reactive] public bool IsCurrentAuto { get; private set; }
        [Reactive] public bool IsCurrentStationary { get; private set; }
    }
}
