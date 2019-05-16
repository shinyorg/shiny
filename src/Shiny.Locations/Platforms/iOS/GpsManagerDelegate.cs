using System;
using System.Linq;
using CoreLocation;
using Foundation;

namespace Shiny.Locations
{
    public class GpsManagerDelegate : CLLocationManagerDelegate
    {
        public GpsRequest Request { get; set; }
        bool deferringUpdates;
        IGpsDelegate gdelegate;


        public event EventHandler<CLLocation[]> UpdatedLocations;
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
        {
            this.deferringUpdates = false;
        }


        void InvokeChanges(CLLocation[] locations)
        {
            if (this.gdelegate == null)
                this.gdelegate = ShinyHost.Resolve<IGpsDelegate>();

            var loc = locations.Last();
            var reading = new GpsReading(loc);
            this.gdelegate?.OnReading(reading);

            //if (this.gdelegate != null)
            //{
            //    foreach (var loc in locations)
            //        this.gdelegate.OnReading(new GpsReading(loc));
            //}
            //this.UpdatedLocations?.Invoke(this, locations);
        }

        //public override void Failed(CLLocationManager manager, NSError error)
        //{
        //    base.Failed(manager, error);
        //}


        public event EventHandler<CLAuthorizationStatus> AuthStatusChanged;
        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
            => this.AuthStatusChanged?.Invoke(this, status);
    }
}
