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
            this.gdelegate = ShinyHost.Resolve<IGpsDelegate>();
            this.readingSubject = new Subject<IGpsReading>();
        }


        readonly Subject<IGpsReading> readingSubject;
        readonly IGpsDelegate gdelegate;
        bool deferringUpdates;

        internal GpsRequest Request { get; set; }


        public IObservable<IGpsReading> WhenGps() => this.readingSubject;
        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            if (this.Request == null)
                this.InvokeChanges(locations);

            else if (!this.deferringUpdates)
            {
                manager.TrySetDeferrals(this.Request);
                this.deferringUpdates = true;
                this.InvokeChanges(locations);
            }
        }


        public override void DeferredUpdatesFinished(CLLocationManager manager, NSError error)
            => this.deferringUpdates = false;


        void InvokeChanges(CLLocation[] locations)
        {
            var loc = locations.Last();
            var reading = new GpsReading(loc);
            this.gdelegate?.OnReading(reading);
            this.readingSubject.OnNext(reading);
        }

        //public override void Failed(CLLocationManager manager, NSError error)
        //{
        //    base.Failed(manager, error);
        //}
    }
}
