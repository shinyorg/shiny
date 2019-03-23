using System;
using System.Reactive.Subjects;
using Android.App;
using Android.Content;
using Android.Gms.Location;


namespace Shiny.Locations
{
    [BroadcastReceiver(
        Name = "com.shiny.locations.GpsBroadcastReceiver",
        Exported = true
    )]
    [IntentFilter(new []
    {
        "com.shiny.locations.GpsBroadcastReceiver.ACTION_PROCESS"
    })]
    public class GpsBroadcastReceiver : BroadcastReceiver
    {
        public const string INTENT_ACTION = "com.shiny.locations.GpsBroadcastReceiver.ACTION_PROCESS";
        public static IObservable<IGpsReading> WhenReading() => readingSubject;
        static readonly Subject<IGpsReading> readingSubject = new Subject<IGpsReading>();

        public override void OnReceive(Context context, Intent intent)
        {
            if (!intent.Action.Equals(INTENT_ACTION))
                return;

            var result = LocationResult.ExtractResult(intent);
            if (result == null)
                return;

            var gpsDelegate = ShinyHost.Resolve<IGpsDelegate>();
            foreach (var location in result.Locations)
            {
                var reading = new GpsReading(location);
                readingSubject.OnNext(reading);
                gpsDelegate?.OnReading(reading);
            }
        }
    }
}
/*
package com.google.android.gms.location.sample.backgroundlocationupdates;

 For apps targeting API level 25 ("Nougat") or lower, location updates may be requested
 using {@link android.app.PendingIntent#getService(Context, int, Intent, int)} or
 {@link android.app.PendingIntent#getBroadcast(Context, int, Intent, int)}. For apps targeting
 API level O, only {@code getBroadcast} should be used.
*/