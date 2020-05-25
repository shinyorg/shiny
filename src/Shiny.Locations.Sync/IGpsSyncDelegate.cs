using System;
using System.Threading.Tasks;


namespace Shiny.Locations.Sync
{
    public interface IGpsSyncDelegate
    {
        Task Process(GpsEvent gpsEvent);
    }
}
