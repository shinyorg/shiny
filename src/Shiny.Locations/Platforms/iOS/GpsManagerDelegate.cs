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
        readonly Lazy<IGpsDelegate> gdelegate = new Lazy<IGpsDelegate>(() => ShinyHost.Resolve<IGpsDelegate>());
        readonly Subject<IGpsReading> readingSubject = new Subject<IGpsReading>();
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


        void InvokeChanges(CLLocation[] locations) => Dispatcher.ExecuteBackgroundTask(async () =>
        {
            var loc = locations.Last();
            var reading = new GpsReading(loc);
            this.gdelegate.Value?.OnReading(reading);
            this.readingSubject.OnNext(reading);
        });


        //public override void Failed(CLLocationManager manager, NSError error)
        //{
        //    base.Failed(manager, error);
        //}
    }
}
