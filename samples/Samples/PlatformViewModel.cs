using System;
using System.Reactive.Linq;
using Shiny;
using Shiny.Net;
using Shiny.Power;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples
{
    public class PlatformViewModel : ViewModel
    {
        readonly IPlatform platform;


        public PlatformViewModel(IPlatform platform,
                                 IConnectivity connectivity,
                                 IPowerManager powerManager)
        {
            this.platform = platform;

            connectivity
                .WhenAnyValue(x => x.Reach)
                .Select(x => x.ToString())
                .ToPropertyEx(this, x => x.NetworkReach)
                .DisposedBy(this.DeactivateWith);

            connectivity
                .WhenAnyValue(x => x.Access)
                .Select(x => x.ToString())
                .ToPropertyEx(this, x => x.NetworkAccess)
                .DisposedBy(this.DeactivateWith);

            connectivity
                .WhenAnyValue(x => x.CellularCarrier)
                .ToPropertyEx(this, x => x.CellularCarrier)
                .DisposedBy(this.DeactivateWith);

            powerManager
                .WhenAnyValue(x => x.IsEnergySavingEnabled)
                .ToPropertyEx(this, x => x.IsEnergySavingEnabled)
                .DisposedBy(this.DeactivateWith);

            powerManager
                .WhenAnyValue(x => x.Status)
                .Select(x => x.ToString())
                .ToPropertyEx(this, x => x.PowerStatus)
                .DisposedBy(this.DeactivateWith);

            powerManager
                .WhenAnyValue(x => x.BatteryLevel)
                .ToPropertyEx(this, x => x.BatteryPercentage)
                .DisposedBy(this.DeactivateWith);
        }


        public string AppIdentifier => this.platform.AppIdentifier;
        public string AppVersion => this.platform.AppVersion;
        public string AppBuild => this.platform.AppBuild;
        public string Name => this.platform.MachineName;
        public string OS => this.platform.OperatingSystem;
        public string OSVersion => this.platform.OperatingSystemVersion;
        public string Device => $"{this.platform.Manufacturer} {this.platform.Model}";
        public string AppData => this.platform.AppData.FullName;
        public string Cache => this.platform.Cache.FullName;
        public string Public => this.platform.Public.FullName;

        public bool IsEnergySavingEnabled { [ObservableAsProperty] get; }
        public string NetworkReach { [ObservableAsProperty] get; }
        public string NetworkAccess { [ObservableAsProperty] get; }
        public string PowerStatus { [ObservableAsProperty] get; }
        public int BatteryPercentage { [ObservableAsProperty] get; }
        public string CellularCarrier { [ObservableAsProperty] get; }
    }
}
