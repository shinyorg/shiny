using System;
using System.Windows.Input;
using Shiny;
using Shiny.Beacons;
using Acr.UserDialogs;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.Beacons
{
    public class RangingViewModel : ViewModel
    {
        readonly IBeaconManager beaconManager;
        readonly IUserDialogs dialogs;
        BeaconRegion region;
        IDisposable scanner;


        public RangingViewModel(INavigationService navigationService,
                                IUserDialogs dialogs,
                                IBeaconManager beaconManager)
        {
            this.dialogs = dialogs;
            this.beaconManager = beaconManager;

            this.Clear = ReactiveCommand.Create(() => this.Beacons.Clear());
            this.SetRegion = ReactiveCommand.CreateFromTask(_ => navigationService.Navigate(
                nameof(CreatePage),
                (nameof(BeaconRegion), this.region),
                ("IsRanging", true)
            ));
            this.ScanToggle = ReactiveCommand.Create(() =>
            {
                if (this.ScanText == "Scan")
                    this.StartScan();
                else
                    this.StopScan();
            });
        }


        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            var currentRegion = parameters.GetValue<BeaconRegion>(nameof(BeaconRegion));
            if (currentRegion != null)
            {
                this.region = currentRegion;

                var txt = "Scanning " + this.region.Uuid;
                if (this.region.Minor != null)
                    txt += $" (M: {this.region.Major}), m: {this.region.Minor})";

                else if (this.region.Major != null)
                    txt += $" (M: {this.region.Major})";

                this.RegionText = txt;
            }
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            if (this.region != null)
                this.StartScan();
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.StopScan();
        }


        public ICommand ScanToggle { get; }
        public ICommand SetRegion { get; }
        public ICommand Clear { get; }

        public ObservableList<BeaconViewModel> Beacons { get; } = new ObservableList<BeaconViewModel>();
        [Reactive] public string RegionText { get; private set; } = "Set Beacon Region";
        [Reactive] public string ScanText { get; private set; } = "Scan";


        void StartScan()
        {
            this.ScanText = "Stop Scan";
            this.Beacons.Clear();

            // TODO
            //this.beaconManager
            //    .RequestAccess()
            //    .Subscribe(result =>
            //    {
            //        if (!result)
            //        {
            //            this.dialogs.Alert("Permission denied");
            //            return;
            //        }
            //        this.scanner = this.beaconManager
            //            .WhenBeaconRanged(this.region)
            //            .ObserveOn(RxApp.MainThreadScheduler)
            //            .Subscribe(
            //                x =>
            //                {
            //                    var beacon = this.Beacons.FirstOrDefault(y => x == y.Beacon);
            //                    if (beacon == null)
            //                        this.Beacons.Add(new BeaconViewModel(x));
            //                    else
            //                        beacon.Proximity = x.Proximity;
            //                },
            //                ex => this.dialogs.Alert(ex.ToString(), "Beacon Scan Error")
            //            );
            //    });

        }


        void StopScan()
        {
            this.ScanText = "Scan";
            this.scanner?.Dispose();
            this.scanner = null;
        }
    }
}