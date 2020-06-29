using System;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.TripTracker.Internals
{
    public class TripTrackerGpsDelegate : IGpsDelegate
    {
        public Task OnReading(IGpsReading reading)
        {
            throw new NotImplementedException();
        }
    }
}
