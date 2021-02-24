using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Beacons;


namespace Shiny.Testing.Beacons
{
    public class TestBeaconMonitoringManager : NotifyPropertyChanged, IBeaconMonitoringManager
    {
        readonly IList<BeaconRegion> regions = new List<BeaconRegion>();

        public event PropertyChangedEventHandler PropertyChanged;

        public Task<IEnumerable<BeaconRegion>> GetMonitoredRegions()
            => Task.FromResult<IEnumerable<BeaconRegion>>(this.regions);


        public AccessState CurrentAccessState { get; set; } = AccessState.Available;
        string? title;
        public string? Title
        {
            get => this.title;
            set => this.Set(ref this.title, value);
        }


        string? message;
        public string? Message
        {
            get => this.message;
            set => this.Set(ref this.message, value);
        }


        bool indeterministic;
        public bool IsIndeterministic
        {
            get => this.indeterministic;
            set => this.Set(ref this.indeterministic, value);
        }


        int progress;
        public int Progress
        {
            get => this.progress;
            set => this.Set(ref this.progress, value);
        }


        int total;
        public int Total
        {
            get => this.total;
            set => this.Set(ref this.total, value);
        }


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
