using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Navigation;

using ReactiveUI;

using Shiny.Beacons;
using Shiny.Beacons.Managed;


namespace Samples.Beacons
{
    public class ManagedBeaconViewModel : ViewModel
    {
        readonly INavigationService navigator;
        readonly ManagedScan scanner;
        BeaconRegion? region;


        public ManagedBeaconViewModel(IBeaconRangingManager beaconManager, INavigationService navigator)
        {
            this.scanner = beaconManager.CreateManagedScan();
            this.navigator = navigator;
            this.SetRegion = navigator.NavigateCommand(
                "CreateBeacon",
                p => p
                    .Set(nameof(BeaconRegion), this.region)
                    .Set("IsRanging", true)
            );
        }


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            this.region = parameters.GetValue<BeaconRegion>(nameof(BeaconRegion));
            if (this.region != null)
            {
                this.scanner.Start(this.region, RxApp.MainThreadScheduler);
                this.IsBusy = true;
            }
        }


        public override void OnDisappearing()
        {
            this.IsBusy = false;
            this.scanner.Stop();
        }


        public ICommand SetRegion { get; }
        public ObservableCollection<ManagedBeacon> Beacons => this.scanner.Beacons;
    }
}
