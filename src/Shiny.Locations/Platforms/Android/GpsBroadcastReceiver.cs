using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Android.App;
using Android.Content;
using Android.Gms.Location;


namespace Shiny.Locations
{
    [BroadcastReceiver(
        Name = GpsManagerImpl.ReceiverName,
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new [] {
        GpsManagerImpl.IntentAction
    })]
    public class GpsBroadcastReceiver : BroadcastReceiver
    {
        public static IObservable<IGpsReading> WhenReading() => readingSubject;
        static readonly Subject<IGpsReading> readingSubject = new Subject<IGpsReading>();


        public override void OnReceive(Context context, Intent intent)
        {
            if (!intent.Action.Equals(GpsManagerImpl.IntentAction))
                return;

            var result = LocationResult.ExtractResult(intent);
            if (result == null)
                return;

            this.Execute(async () =>
            {
                var delegates = ShinyHost.Resolve<IEnumerable<IGpsDelegate>>();
                foreach (var location in result.Locations)
                {
                    var reading = new GpsReading(location);

                    await delegates
                        .RunDelegates(x => x.OnReading(reading))
                        .ConfigureAwait(false);
                    
                    readingSubject.OnNext(reading);
                }
            });
        }
    }
}