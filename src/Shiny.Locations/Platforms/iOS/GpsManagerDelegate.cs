using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreLocation;
using Foundation;


namespace Shiny.Locations
{
    public class GpsManagerDelegate : ShinyLocationDelegate
    {
        public GpsManagerDelegate()
        {
            this.readingSubject = new Subject<IGpsReading>();
        }


        readonly Subject<IGpsReading> readingSubject;
        bool deferringUpdates;

        internal GpsRequest Request { get; set; }


        public IObservable<IGpsReading> WhenGps() => this.readingSubject;
        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            if (this.Request?.ThrottledInterval == null)
                this.InvokeChanges(locations);

            else if (!this.deferringUpdates)
            {
                manager.AllowDeferredLocationUpdatesUntil(0, this.Request.ThrottledInterval.Value.TotalMilliseconds);
                this.deferringUpdates = true;
                this.InvokeChanges(locations);
            }
        }


        public override void DeferredUpdatesFinished(CLLocationManager manager, NSError error)
            => this.deferringUpdates = false;


        void InvokeChanges(CLLocation[] locations) => Dispatcher.Execute(async () =>
        {
            var loc = locations.Last();
            var reading = new GpsReading(loc);
            ShinyHost.Resolve<IGpsDelegate>()?.OnReading(reading);
            this.readingSubject.OnNext(reading);
        });


        //public override void Failed(CLLocationManager manager, NSError error)
        //{
        //    base.Failed(manager, error);
        //}
    }
}
