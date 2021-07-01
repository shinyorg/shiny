using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using CoreLocation;
using Shiny.Locations.Infrastructure;
#if __IOS__
using UIKit;
#endif

namespace Shiny.Locations
{
    public class GeofenceManagerImpl : AbstractGeofenceManager
    {
        readonly CLLocationManager locationManager;
        readonly GeofenceManagerDelegate gdelegate;


        public GeofenceManagerImpl(IRepository repository) : base(repository)
        {
            this.gdelegate = new GeofenceManagerDelegate();
            this.locationManager = new CLLocationManager { Delegate = this.gdelegate };
        }


        public override Task<AccessState> RequestAccess()
            => this.locationManager.RequestAccess(true);


        public override async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
        {
            var task = this.gdelegate
                .WhenStateDetermined()
                .Where(x => region.Equals(x.Region))
                .Take(1)
                .Select(x => x.Status)
                .Timeout(TimeSpan.FromSeconds(20))
                .ToTask(cancelToken);

            this.locationManager.RequestState(region.ToNative());
            try
            {
                var result = await task.ConfigureAwait(false);
                return result;
            }
            catch (TimeoutException ex)
            {
                throw new TimeoutException("Could not retrieve latest GPS coordinates to be able to determine geofence current state", ex);
            }
        }


        public override async Task StartMonitoring(GeofenceRegion region)
        {
            var access = await this.RequestAccess();
            access.Assert();

            var native = region.ToNative();

#if __IOS__
            var tcs = new TaskCompletionSource<object>();
            UIApplication.SharedApplication.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    this.locationManager.StartMonitoring(native);
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    this.locationManager.StopMonitoring(native);
                    tcs.SetException(ex);
                }
            });
            await tcs.Task.ConfigureAwait(false);
#else
            this.locationManager.StartMonitoring(native);
#endif
            await this.Repository.Set(region.Identifier, region);
        }


        public override async Task StopMonitoring(string identifier)
        {
            var region = await this.Repository.Get(identifier);
            if (region != null)
            {
                await this.Repository.Remove(region.Identifier);
                this.locationManager.StopMonitoring(region.ToNative());
            }
        }


        public override async Task StopAllMonitoring()
        {
            await this.Repository.Clear();
            var natives = this
                .locationManager
                .MonitoredRegions
                .OfType<CLCircularRegion>()
                .ToList();

            foreach (var native in natives)
                this.locationManager.StopMonitoring(native);
        }
    }
}