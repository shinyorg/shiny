using System;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Android.Runtime;

namespace Shiny.Locations;


public class ShinyLocationCallback : LocationCallback, Android.Locations.ILocationListener
{
    public Action<Location>? OnReading { get; set; }


    public void OnLocationChanged(Location? location)
    {
        if (location != null)
            this.OnReading?.Invoke(location);
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
