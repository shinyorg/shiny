using System;
using System.Reactive.Linq;
using Shiny;
using Shiny.Net;
using Shiny.Power;
using ReactiveUI;


namespace Samples
{
    public class EnvironmentViewModel : ViewModel
    {
        readonly IEnvironment environment;


        public EnvironmentViewModel(IEnvironment environment,
                                    IConnectivity connectivity,
                                    IPowerManager powerManager)
        {
            this.environment = environment;

            this.networkReach = connectivity
                .WhenAnyValue(x => x.Reach)
                .Select(x => x.ToString())
                .ToProperty(this, x => x.NetworkReach);

            this.networkAccess = connectivity
                .WhenAnyValue(x => x.Access)
                .Select(x => x.ToString())
                .ToProperty(this, x => x.NetworkAccess);

            this.powerStatus = powerManager
                .WhenAnyValue(x => x.Status)
                .Select(x => x.ToString())
                .ToProperty(this, x => x.PowerStatus);

            this.batteryPercentage = powerManager
                .WhenAnyValue(x => x.BatteryLevel)
                .ToProperty(this, x => x.BatteryPercentage);
        }


        public string AppIdentifier => this.environment.AppIdentifier;
        public string AppVersion => this.environment.AppVersion;
        public string AppBuild => this.environment.AppBuild;
        public string Name => this.environment.MachineName;
        public string OS => this.environment.OperatingSystem;
        public string Device => $"{this.environment.Manufacturer} {this.environment.Model}";

        readonly ObservableAsPropertyHelper<string> networkReach;
        public string NetworkReach => this.networkReach.Value;

        readonly ObservableAsPropertyHelper<string> networkAccess;
        public string NetworkAccess => this.networkAccess.Value;

        readonly ObservableAsPropertyHelper<string> powerStatus;
        public string PowerStatus => this.powerStatus.Value;

        readonly ObservableAsPropertyHelper<int> batteryPercentage;
        public int BatteryPercentage => this.batteryPercentage.Value;
    }
}
