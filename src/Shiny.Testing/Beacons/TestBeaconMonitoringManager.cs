using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Beacons;


namespace Shiny.Testing.Beacons
{
    public class TestBeaconMonitoringManager : IBeaconMonitoringManager
    {
        readonly IList<BeaconRegion> regions = new List<BeaconRegion>();
        public Task<IEnumerable<BeaconRegion>> GetMonitoredRegions()
            => Task.FromResult<IEnumerable<BeaconRegion>>(this.regions);


        public AccessState CurrentAccessState { get; set; } = AccessState.Available;
        public Task<AccessState> RequestAccess() => Task.FromResult(this.CurrentAccessState);


        public Task StartMonitoring(BeaconRegion region)
        {
            this.regions.Add(region);
            return Task.CompletedTask;
        }


        public Task StopAllMonitoring()
        {
            this.regions.Clear();
            return Task.CompletedTask;
        }


        public Task StopMonitoring(string identifier)
        {
            var region = this.regions.FirstOrDefault(x => x.Identifier.Equals(identifier, StringComparison.InvariantCultureIgnoreCase));
            if (region != null)
                this.regions.Remove(region);

            return Task.CompletedTask;
        }
    }
}
