using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Beacons;


namespace Shiny.Testing.Beacons
{
    public class BeaconMonitoringManager : IBeaconMonitoringManager
    {
        readonly IList<BeaconRegion> monitoredRegions = new List<BeaconRegion>();
        public Task<IEnumerable<BeaconRegion>> GetMonitoredRegions() => Task.FromResult(this.monitoredRegions.AsEnumerable());


        public AccessState AccessStatus { get; set; } = AccessState.Available;
        public Task<AccessState> RequestAccess() => Task.FromResult(this.AccessStatus);


        public Task StartMonitoring(BeaconRegion region)
        {
            this.monitoredRegions.Add(region);
            return Task.CompletedTask;
        }


        public Task StopAllMonitoring()
        {
            this.monitoredRegions.Clear();
            return Task.CompletedTask;
        }


        public Task StopMonitoring(string identifier)
        {
            var region = this.monitoredRegions.FirstOrDefault(x => x.Identifier == identifier);
            if (region != null)
                this.monitoredRegions.Remove(region);

            return Task.CompletedTask;
        }
    }
}
