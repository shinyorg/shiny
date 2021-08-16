using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Locations;
using Microsoft.Extensions.Logging;
using AContext = Android.Content.Context;


namespace Shiny.Locations
{
    public class LocationServicesGpsManagerImpl : AbstractGpsManager
    {
        readonly LocationManager client;
        public LocationServicesGpsManagerImpl(IAndroidContext context, ILogger<LocationServicesGpsManagerImpl> logger) : base(context, logger)
            => this.client = context.GetSystemService<LocationManager>(AContext.LocationService);


        public override IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync(async ct =>
        {
            (await this.RequestAccess(GpsRequest.Foreground)).Assert(null, true);

            var criteria = new Criteria
            {
                BearingRequired = false,
                AltitudeRequired = false,
                SpeedRequired = false
            };
            var location = this.client.GetLastKnownLocation(this.client.GetBestProvider(criteria, false));
            if (location != null)
                return new GpsReading(location);

            return null;
        });


        protected override Task RequestLocationUpdates(GpsRequest request)
        {
            var criteria = new Criteria
            {
                BearingRequired = true,
                AltitudeRequired = true,
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
