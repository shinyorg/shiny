using System;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;


namespace Shiny.Locations
{
    [Service(
        Enabled = true,
        Exported = true,
        ForegroundServiceType = ForegroundService.TypeLocation
    )]
    public class ShinyGpsService : ShinyAndroidForegroundService<IGpsManager, IGpsDelegate>
    {
        protected override void OnStart(Intent? intent)
        {
            var request = new GpsRequest(); // TODO: grab from intent
            var gpsObs = this.Service.WhenReading();

            if (request.ThrottledInterval != null)
                gpsObs.Sample(request.ThrottledInterval.Value);

            if (request.MinimumDistance != null)
            {
                gpsObs
                    .WithPrevious()
                    .Where(x => x.Item1
                        .Position
                        .GetDistanceTo(x.Item2.Position).TotalMeters >= request.MinimumDistance.TotalMeters
                    )
                    .Select(x => x.Item2);
            }

            gpsObs
                .SubscribeAsync(
                    reading => this.Delegates.RunDelegates(
                        x => x.OnReading(reading)
                    )
                )
                .DisposedBy(this.DestroyWith);
        }


        public override IBinder? OnBind(Intent? intent) => null;
    }
}
