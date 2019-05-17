using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.Testing.Locations
{
    public class TestGeofenceManager : IGeofenceManager
    {
        readonly Subject<AccessState> accessSubject = new Subject<AccessState>();

        AccessState status;
        public AccessState Status
        {
            get => this.status;
            set
            {
                this.status = value;
                this.accessSubject.OnNext(value);
            }
        }


        readonly IList<GeofenceRegion> regions = new List<GeofenceRegion>();
        public IReadOnlyList<GeofenceRegion> MonitoredRegions => this.regions.ToList();


        public Task<IEnumerable<GeofenceRegion>> GetMonitorRegions()
            => Task.FromResult<IEnumerable<GeofenceRegion>>(this.MonitoredRegions);


        public Task<AccessState> RequestAccess() => Task.FromResult(this.Status);


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

        public IObservable<AccessState> WhenAccessStatusChanged()
        {
            throw new NotImplementedException();
        }
    }
}
