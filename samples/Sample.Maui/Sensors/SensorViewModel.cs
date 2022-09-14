using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Shiny.Sensors;
using Xamarin.Forms;


namespace Sample
{
    public class SensorViewModel<TReading> : Shiny.NotifyPropertyChanged, ISensorViewModel
    {
        IDisposable? sensorSub;


        public SensorViewModel(ISensor<TReading> sensor, string valueName, string? title = null)
        {
            this.Title = title ?? sensor.GetType().Name.Replace("Impl", String.Empty);
            this.ValueName = valueName;
            this.ToggleText = sensor.IsAvailable ? "Start" : "Sensor Not Available";

            this.Toggle = new Command(() =>
            {
                if (!sensor.IsAvailable)
                    return;

                if (this.sensorSub == null)
                {
                    this.ToggleText = "Stop";
                    this.sensorSub = sensor
                        .WhenReadingTaken()
                        .Sample(TimeSpan.FromMilliseconds(500))
                        .Subscribe(x => this.Value = x!.ToString());
                }
                else
                {
                    this.ToggleText = "Start";
                    this.sensorSub.Dispose();
                    this.sensorSub = null;
                }
            });
        }


        public string? Title { get; }
        public ICommand Toggle { get; }
        public string? ValueName { get; }


        string? value;
        public string? Value
        {
            get => this.value;
            private set => this.Set(ref this.value, value);
        }


        string? toggleText;
        public string? ToggleText
        {
            get => this.toggleText;
            private set => this.Set(ref this.toggleText, value);
        }
    }
}
