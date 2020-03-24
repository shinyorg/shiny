using System;
using System.Linq;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Hardware;


namespace Shiny.Sensors
{
    public abstract class AbstractSensor<T>
    {
		readonly SensorManager sensorManager;
		readonly SensorType type;
        readonly IObservable<T> readOb;


        protected AbstractSensor(SensorType type)
        {
            this.type = type;
			this.sensorManager = (SensorManager)Application.Context.GetSystemService(Context.SensorService);
			this.IsAvailable = this.sensorManager.GetSensorList(type).Any();

            this.readOb = Observable.Create<T>(ob =>
            {
                var mgr = new ShinySensorManager(this.sensorManager);
                //var delay = this.ToSensorDelay(this.ReportInterval);

                mgr.Start(this.type, SensorDelay.Fastest, e =>
                {
                    var reading = this.ToReading(e);
                    ob.OnNext(reading);
                });
                return () => mgr.Stop();
            })
            .Publish()
            .RefCount();
        }


        protected abstract T ToReading(SensorEvent e);
		public bool IsAvailable { get; }
        public IObservable<T> WhenReadingTaken() => this.readOb;
        //protected SensorDelay ToSensorDelay(TimeSpan timeSpan)
        //{
        //    if (timeSpan.TotalMilliseconds <= 100)
        //        return SensorDelay.Fastest;

        //    if (timeSpan.TotalMilliseconds <= 250)
        //        return SensorDelay.Game;

        //    if (timeSpan.TotalMilliseconds <= 500)
        //        return SensorDelay.Ui;

        //   return SensorDelay.Normal;
        //}
    }
}