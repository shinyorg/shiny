using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.Testing.Locations
{
    public class TestGeofenceManager : IGeofenceManager
    {
        public AccessState Status { get; set; }

        readonly IList<GeofenceRegion> regions = new List<GeofenceRegion>();
        public IReadOnlyList<GeofenceRegion> MonitoredRegions => this.regions.ToList();


        public Task<IEnumerable<GeofenceRegion>> GetMonitorRegions()
            => Task.FromResult<IEnumerable<GeofenceRegion>>(this.MonitoredRegions);


        public AccessState RequestAccessReply { get; set; } = AccessState.Available;
        public Task<AccessState> RequestAccess() => Task.FromResult(this.RequestAccessReply);


        public IDictionary<GeofenceRegion, GeofenceState> GeofenceRegionStates { get; } = new Dictionary<GeofenceRegion, GeofenceState>();
        public Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
        {
            if (this.GeofenceRegionStates.ContainsKey(region))
                return Task.FromResult(this.GeofenceRegionStates[region]);

            return Task.FromResult(GeofenceState.Unknown);
        }


        public Task StartMonitoring(GeofenceRegion region)
        {
            this.regions.Add(region);
            return Task.CompletedTask;
        }


        public Task StopAllMonitoring()
        {
            this.regions.Clear();
            return Task.CompletedTask;
        }


        public Task StopMonitoring(GeofenceRegion region)
        {
            this.regions.Remove(region);
            return Task.CompletedTask;
        }
    }
}
