using System;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.Locations.Sync
{
    public class SyncGpsDelegate : IGpsDelegate
    {
        public Task OnReading(IGpsReading reading)
        {
            throw new NotImplementedException();
        }
    }
}
