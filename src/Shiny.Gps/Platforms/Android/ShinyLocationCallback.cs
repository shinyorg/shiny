using System;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Android.Runtime;

namespace Shiny.Locations;


public class ShinyLocationCallback : LocationCallback, Android.Locations.ILocationListener
{
    readonly StationaryDetector detector = new();

    public Action<GpsReading>? OnReading { get; set; }
    public int StationaryMetersThreshold { get; set; } = 10;
    public int StationarySecondsThreshold { get; set; } = 30;


    // a new listener must not compare its first reading against where the last one stopped
    internal void ResetStationary() => this.detector.Reset();


    public void OnLocationChanged(Location? location)
    {
        if (location == null)
            return;

        var reading = location.FromNative();
        var isStationary = this.detector.Detect(reading, this.StationaryMetersThreshold, this.StationarySecondsThreshold);
        if (isStationary)
            reading = reading with { IsStationary = true };

        this.OnReading?.Invoke(reading);
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
