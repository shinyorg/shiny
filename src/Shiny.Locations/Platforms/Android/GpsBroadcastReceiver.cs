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
        GpsBroadcastReceiver.INTENT_ACTION
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

            Dispatcher.SmartExecuteSync(async () =>
            {
                var gpsDelegate = ShinyHost.Resolve<IGpsDelegate>();
                foreach (var location in result.Locations)
                {
                    var reading = new GpsReading(location);
                    readingSubject.OnNext(reading);
                    if (gpsDelegate != null)
                        await gpsDelegate
                            .OnReading(reading)
                            .ConfigureAwait(false);
                }
            });
        }
    }
}