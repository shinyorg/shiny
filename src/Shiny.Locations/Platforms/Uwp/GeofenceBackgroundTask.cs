using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Shiny.Infrastructure;


namespace Shiny.Locations
{
    public class GeofenceBackgroundTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var reports = GeofenceMonitor
                .Current
                .ReadReports()
                .Where(x => x.Geofence.Geoshape is Geocircle);

            var repo = ShinyHost.Resolve<IRepository>();
            var gfDelegate = ShinyHost.Resolve<IGeofenceDelegate>();

            foreach (var report in reports)
            {
                var region = await repo.Get<GeofenceRegion>(report.Geofence.Id);

                var newStatus = this.FromNative(report.NewState);
                gfDelegate.OnStatusChanged(newStatus, region);

                if (region.SingleUse)
                    await repo.Remove<GeofenceRegion>(region.Identifier);
            }
        }


        GeofenceState FromNative(Windows.Devices.Geolocation.Geofencing.GeofenceState state)
        {
            switch (state)
            {
                case Windows.Devices.Geolocation.Geofencing.GeofenceState.Entered:
                    return GeofenceState.Entered;

                case Windows.Devices.Geolocation.Geofencing.GeofenceState.Exited:
                    return GeofenceState.Exited;

                default:
                    return GeofenceState.Unknown;
            }
        }
    }
}
