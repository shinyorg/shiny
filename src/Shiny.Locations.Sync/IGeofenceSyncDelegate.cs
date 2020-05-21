using System;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.Locations.Sync
{
    public interface IGeofenceSyncDelegate
    {
        Task Process(GeofenceRegion region, DateTimeOffset createdAt);
    }
}
