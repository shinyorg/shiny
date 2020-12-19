using System;
using System.Linq;
using System.Windows.Input;
using System.Reactive.Linq;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;
using Shiny.Beacons;
using Samples.Infrastructure;


namespace Samples.Beacons
{
    public class RangingViewModel : ViewModel
    {
        readonly IBeaconRangingManager beaconManager;
        readonly IDialogs dialogs;
        BeaconRegion? region;
        IDisposable? scanner;


        public RangingViewModel(INavigationService navigator,
                                IDialogs dialogs,
                                IBeaconRangingManager beaconManager)
        {
            this.dialogs = dialogs;
            this.beaconManager = beaconManager;

            this.WhenAnyValue(x => x.Uuid)
                .Select(x => !x.IsEmpty())
                .ToPropertyEx(this, x => x.IsRegionSet);

            this.WhenAnyValue(x => x.Major)
                .Select(x => !x.IsEmpty())
                .ToPropertyEx(this, x => x.IsMajorSet);

            this.WhenAnyValue(x => x.Minor)
                .Select(x => !x.IsEmpty())
                .ToPropertyEx(this, x => x.IsMinorSet);

            this.SetRegion = navigator.NavigateCommand(
                "CreateBeacon",
                p => p
                    .Set(nameof(BeaconRegion), this.region)
                    .Set("IsRanging", true)
            );
            this.ScanToggle = ReactiveCommand.Create(() =>
            {
                if (this.scanner == null)
                    this.StartScan();
                else
                    this.StopScan();
            });
        }


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
            var currentRegion = parameters.GetValue<BeaconRegion>(nameof(BeaconRegion));
            if (currentRegion != null)
            {
                this.region = currentRegion;
                this.Uuid = currentRegion.Uuid.ToString();
                this.Major = currentRegion.Major?.ToString();
                this.Minor = currentRegion.Minor?.ToString();

                this.StartScan();
            }
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.StopScan();
        }


        public ICommand ScanToggle { get; }
        public ICommand SetRegion { get; }
        public ObservableList<BeaconViewModel> Beacons { get; } = new ObservableList<BeaconViewModel>();

        public bool IsRegionSet { [ObservableAsProperty] get; }
        public bool IsMajorSet { [ObservableAsProperty] get; }
        public bool IsMinorSet { [ObservableAsProperty] get; }
        [Reactive] public string Uuid { get; private set; }
        [Reactive] public string Major { get; private set; }
        [Reactive] public string Minor { get; private set; }
        [Reactive] public string ScanText { get; private set; } = "Scan";


        void StartScan()
        {
            this.ScanText = "Stop Scan";
            this.Beacons.Clear();

            this.scanner = this.beaconManager
                .WhenBeaconRanged(this.region)
                .Synchronize()
                .SubOnMainThread(
                    x =>
                    {
                        var beacon = this.Beacons.FirstOrDefault(y => x.Equals(y.Beacon));
                        if (beacon == null)
                            this.Beacons.Add(new BeaconViewModel(x));
                        else
                            beacon.Proximity = x.Proximity;
                    },
                    ex => this.dialogs.Alert(ex.ToString(), "Beacon Scan Error")
                );
        }


        void StopScan()
        {
            this.ScanText = "Scan";
            this.scanner?.Dispose();
            this.scanner = null;
        }
    }
}