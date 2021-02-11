using System;
using System.Linq;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Shiny;
using Shiny.Infrastructure;
using Microsoft.Extensions.Logging;


namespace Shiny.Locations
{
    public class GeofenceBackgroundTaskProcessor : IBackgroundTaskProcessor
    {
        readonly IEnumerable<IGeofenceDelegate> delegates;
        readonly IRepository repository;
        readonly ILogger logger;


        public GeofenceBackgroundTaskProcessor(IRepository repository,
                                               ILogger<IGeofenceDelegate> logger,
                                               IEnumerable<IGeofenceDelegate> delegates)
        {
            this.repository = repository;
            this.logger = logger;
            this.delegates = delegates;
        }


        public async void Process(IBackgroundTaskInstance taskInstance)
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

                    await this.delegates.RunDelegates(
                        x => x.OnStatusChanged(newStatus, region),
                        ex => this.logger.LogError(ex, "Error in geofence delegate")
                    );

                    if (region.SingleUse)
                        await this.repository.Remove<GeofenceRegion>(region.Identifier);
                }
            }
            deferral.Complete();
        }
    }
}
