using Shiny.Locations;

using System;
using System.Threading.Tasks;

namespace Shiny.Services.LocationSync
{
    public class SyncGpsDelegate : IGpsDelegate
    {
        public Task OnReading(IGpsReading reading)
        {
            throw new NotImplementedException();
        }
    }
}
