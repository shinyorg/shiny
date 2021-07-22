using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Locations;
using AContext = Android.Content.Context;


namespace Shiny.Locations
{
    public class LocationServicesGpsManagerImpl : AbstractGpsManager
    {
        readonly LocationManager client;
        public LocationServicesGpsManagerImpl(IAndroidContext context) : base(context)
            => this.client = context.GetSystemService<LocationManager>(AContext.LocationService);


        public override IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync(async ct =>
        {
            (await this.RequestAccess(GpsRequest.Foreground)).Assert(null, true);

            var criteria = new Criteria
            {
                BearingRequired = false,
                AltitudeRequired = false,
                SpeedRequired = true
            };
            var location = this.client.GetLastKnownLocation(this.client.GetBestProvider(criteria, false));
            if (location != null)
                return new GpsReading(location);

            try
            {
                var task = this.WhenReading().Take(1).ToTask(ct);
                await this.RequestLocationUpdates(GpsRequest.Foreground);
                var reading = await task.ConfigureAwait(false);
                return reading;
            }
            finally
            {
                await this.RemoveLocationUpdates();
            }            
        });


        protected override Task RequestLocationUpdates(GpsRequest request)
        {
            var criteria = new Criteria
            {
                BearingRequired = false,
                AltitudeRequired = false,
                SpeedRequired = true
            };

            this.client.RequestLocationUpdates(
                (long)request.Interval.TotalMilliseconds,
                (float)(request.MinimumDistance?.TotalMeters ?? 0),
                criteria,
                this.Callback,
                null
            );
            return Task.CompletedTask;
        }


        protected override Task RemoveLocationUpdates()
        {
            this.client.RemoveUpdates(this.Callback);
            return Task.CompletedTask;
        }
    }
}
