using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Locations
{
    public class GeofenceBackgroundTask : IBackgroundTask
    {
        readonly IGeofenceDelegate gdelegate;
        readonly IRepository repository;


        public GeofenceBackgroundTask()
        {
            this.gdelegate = ShinyHost.Resolve<IGeofenceDelegate>();
            this.repository = ShinyHost.Resolve<IRepository>();
        }


        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var reports = GeofenceMonitor
                .Current
                .ReadReports()
                .Where(x => x.Geofence.Geoshape is Geocircle);

            foreach (var report in reports)
            {
                var region = await this.repository.Get<GeofenceRegion>(report.Geofence.Id);

                var newStatus = report.NewState.FromNative();
                this.FireDelegate(newStatus, region);

                if (region.SingleUse)
                    await this.repository.Remove<GeofenceRegion>(region.Identifier);
            }
        }


        void FireDelegate(GeofenceState state, GeofenceRegion region)
        {
            try
            {
                this.gdelegate.OnStatusChanged(state, region);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
