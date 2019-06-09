using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

                var newStatus = report.NewState.FromNative();
                this.FireDelegate(newStatus, region);

                if (region.SingleUse)
                    await this.repository.Remove<GeofenceRegion>(region.Identifier);
            }
            deferral.Complete();
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
