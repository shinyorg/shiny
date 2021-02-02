using System;
using System.Reactive.Subjects;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Android.Runtime;


namespace Shiny.Locations
{
    public class ShinyLocationCallback : LocationCallback, Android.Locations.ILocationListener
    {
        public Subject<GpsReading> ReadingSubject { get; } = new Subject<GpsReading>();


        public void OnLocationChanged(Android.Locations.Location location)
        {
            var reading = new GpsReading(location);
            this.ReadingSubject.OnNext(reading);
        }


        public override void OnLocationResult(LocationResult result)
        {
            foreach (var location in result.Locations)
                this.OnLocationChanged(location);
        }


        public void OnProviderDisabled(string? provider) { }
        public void OnProviderEnabled(string? provider) { }
        public void OnStatusChanged(string? provider, [GeneratedEnum] Availability status, Bundle? extras) { }
    }
}
