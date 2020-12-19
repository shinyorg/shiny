using System;
using System.Threading.Tasks;
using Samples.Infrastructure;
using Samples.Models;
using Shiny;
using Shiny.Locations;


namespace Samples.Geofences
{
    public class GeofenceDelegate : IGeofenceDelegate, IShinyStartupTask
    {
        readonly CoreDelegateServices services;
        public GeofenceDelegate(CoreDelegateServices services) => this.services = services;


        public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            await this.services.Connection.InsertAsync(new GeofenceEvent
            {
                Identifier = region.Identifier,
                Entered = newStatus == GeofenceState.Entered,
                Date = DateTime.Now
            });
            await this.services.Notifications.Send(
                this.GetType(),
                newStatus == GeofenceState.Entered,
                "Geofence Event",
                $"{region.Identifier} was {newStatus}"
            );
        }


        public void Start()
            => this.services.Notifications.Register(this.GetType(), true, "Geofences");
    }
}
