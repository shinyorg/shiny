using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Locations.Sync;
using Shiny.TripTracker;

namespace Shiny.Generators.Tests
{
    public class MyGeofenceSyncDelegate : IGeofenceSyncDelegate
    {
        public Task Process(IEnumerable<GeofenceEvent> geofence, CancellationToken cancelToken) => throw new NotImplementedException();
    }

    public class MyTripTrackerDelegate : ITripTrackerDelegate
    {
        public Task OnTripStart(Trip trip) => throw new NotImplementedException();

        public Task OnTripEnd(Trip trip) => throw new NotImplementedException();
    }
}
