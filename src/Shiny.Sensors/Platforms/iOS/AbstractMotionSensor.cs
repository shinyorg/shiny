using System;
using System.Reactive.Linq;
using CoreMotion;


namespace Shiny.Sensors
{
    public abstract class AbstractMotionSensor
    {
        readonly CMMotionManager motionManager = new CMMotionManager();


        protected abstract bool IsSensorAvailable(CMMotionManager mgr);
        protected abstract void Start(CMMotionManager mgr, IObserver<MotionReading> ob);
        protected abstract void Stop(CMMotionManager mgr);
        protected abstract void SetReportInterval(CMMotionManager mgr, TimeSpan timeSpan);


        public bool IsAvailable => this.IsSensorAvailable(this.motionManager);


		TimeSpan reportInterval = TimeSpan.FromMilliseconds(500);
        public TimeSpan ReportInterval
        {
            get => this.reportInterval;
            set
            {
                this.reportInterval = value;
                this.SetReportInterval(this.motionManager, value);
            }
        }


        IObservable<MotionReading> readOb;
        public IObservable<MotionReading> WhenReadingTaken()
        {
            this.readOb = this.readOb ?? Observable.Create<MotionReading>(ob =>
            {
                this.Start(this.motionManager, ob);
                return () => this.Stop(this.motionManager);
            })
            .Publish()
            .RefCount();

            return this.readOb;
        }
    }
}