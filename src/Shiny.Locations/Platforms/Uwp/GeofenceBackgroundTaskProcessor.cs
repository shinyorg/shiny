using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Locations
{
    public class GeofenceBackgroundTaskProcessor : IBackgroundTaskProcessor
    {
        readonly IGeofenceDelegate gdelegate;
        readonly IRepository repository;


        public GeofenceBackgroundTaskProcessor(IGeofenceDelegate gdelegate, IRepository repository)
        {
            this.gdelegate = gdelegate;
            this.repository = repository;
        }


        public async void Process(string taskName, IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            // cancellation?

            var reports = GeofenceMonitor
                .Current
                .ReadReports()
                .Where(x => x.Geofence.Geoshape is Geocircle);

            foreach (var report in reports)
            {
                var region = await this.repository.Get<GeofenceRegion>(report.Geofence.Id);
                if (region != null)
                {
                    var newStatus = report.NewState.FromNative();
                    await Log.SafeExecute(() => this.gdelegate.OnStatusChanged(newStatus, region));

                    if (region.SingleUse)
                        await this.repository.Remove<GeofenceRegion>(region.Identifier);
                }
            }
            deferral.Complete();
        }
    }
}
