using System;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Android.Runtime;

namespace Shiny.Locations;


public class ShinyLocationCallback : LocationCallback, Android.Locations.ILocationListener
{
    public Action<Location>? OnReading { get; set; }


    // TODO: calculate stationary here
        // TODO: need to pass to gpsreading
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
/*
DateTimeOffset? lastMovement;
   
   internal void DetectIfStationary(GpsReading reading)
   {
       this.lastMovement ??= reading.Timestamp;

       if (this.MostRecentReading != null)
       {
           var distance = reading.Position.GetDistanceTo(this.lastReading.Position);
           if (distance.TotalMeters < StationaryMetersThreshold)
           {
               var time = reading.Timestamp - this.lastMovement;
               if (time.Value.TotalSeconds >= StationarySecondsThreshold)
               {
                   if (this.IsStationary)
                   {
                       logger.LogDebug("Still stationary");
                   }
                   else
                   {
                       this.IsStationary = true;
                       logger.LogDebug("Stationary Detected");
                   }
               }
               else
               {
                   logger.LogDebug("Stationary Detected, but insufficient time has past: " + time);
               }
           }
           else
           {
               logger.LogDebug("Stationary Threshold Not Reached - {Meters}m", distance.TotalMeters);
               this.lastMovement = reading.Timestamp;
               if (this.IsStationary)
               {
                   this.IsStationary = false;
                   logger.LogDebug("Stationary Changed to In-Motion");
               }
               else
               {
                   logger.LogDebug("Still in-motion");
               }
           }
       }
   }
   
   
   protected int StationaryMetersThreshold { get; set; } = 10;
   protected int StationarySecondsThreshold { get; set; } = 30;
   protected bool DetectStationary { get; set; }
 */