using System;
using CoreLocation;


namespace Shiny.Locations
{
    public class GpsManagerDelegate : CLLocationManagerDelegate
    {
        IGpsDelegate gdelegate;
        //bool loaded;


        public event EventHandler<CLLocation[]> UpdatedLocations;
        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            if (this.gdelegate == null)
                this.gdelegate = ShinyHost.Resolve<IGpsDelegate>();

            if (this.gdelegate != null)
            {
                foreach (var loc in locations)
                    this.gdelegate.OnReading(new GpsReading(loc));
            }
            this.UpdatedLocations?.Invoke(this, locations);
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
