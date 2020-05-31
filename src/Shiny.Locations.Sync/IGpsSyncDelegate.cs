using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Locations.Sync
{
    public interface IGpsSyncDelegate
    {
        Task Process(IEnumerable<GpsEvent> gpsEvent);
    }
}
