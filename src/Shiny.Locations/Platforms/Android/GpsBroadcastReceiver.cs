using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;

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
        GpsManagerImpl.IntentAction,
        Intent.ActionBootCompleted
    })]
    public class GpsBroadcastReceiver : ShinyBroadcastReceiver
    {
        public static IObservable<IGpsReading> WhenReading() => readingSubject;
        static readonly Subject<IGpsReading> readingSubject = new Subject<IGpsReading>();


        protected override async Task OnReceiveAsync(Context context, Intent intent)
        {
            // if boot completed received & gps background was on, this broadcastreceiver will cause the application to spinup the
            // shiny infrastructure and thus the GPS background monitoring
            if (!intent.Action.Equals(GpsManagerImpl.IntentAction))
                return;

            var result = LocationResult.ExtractResult(intent);
            if (result == null)
                return;

            var delegates = ShinyHost.Resolve<IEnumerable<IGpsDelegate>>();
            foreach (var location in result.Locations)
            {
                var reading = new GpsReading(location);

                await delegates
                    .RunDelegates(x => x.OnReading(reading))
                    .ConfigureAwait(false);

                readingSubject.OnNext(reading);
            }
        }
    }
}