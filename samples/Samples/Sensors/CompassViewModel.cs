using System;
using System.Reactive.Disposables;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny.Sensors;


namespace Samples.Sensors
{
    public class CompassViewModel : ViewModel
    {
        readonly ICompass compass;
        readonly IDialogs dialogs;


        public CompassViewModel(IDialogs dialogs, ICompass compass)
        {
            this.compass = compass;
            this.dialogs = dialogs;
        }


        [Reactive] public double Rotation { get; private set; }
        [Reactive] public double Heading { get; private set; }


        public override async void OnAppearing()
        {
            base.OnAppearing();
            if (!this.compass.IsAvailable)
            {
                await this.dialogs.Alert("Compass is not available");
                return;
            }
            this.compass
                .WhenReadingTaken()
                .SubOnMainThread(x =>
                {
                    this.Rotation = 360 - x.MagneticHeading;
                    this.Heading = x.MagneticHeading;
                })
                .DisposeWith(this.DeactivateWith);
        }
    }
}
