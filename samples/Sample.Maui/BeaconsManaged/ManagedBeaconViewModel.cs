using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;
using Shiny.Beacons;
using Shiny.Beacons.Managed;


namespace Sample
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
                "CreatePage",
                p => p
                    .Set(nameof(BeaconRegion), this.region)
                    .Set("IsRanging", true)
            );
        }


        public override async void OnNavigatedTo(INavigationParameters parameters)
        {
            this.region = parameters.GetValue<BeaconRegion>(nameof(BeaconRegion));
            if (this.region != null)
            {
                try
                {
                    await this.scanner.Start(this.region, RxApp.MainThreadScheduler);
                    this.Uuid = this.region.Uuid.ToString();
                    this.Major = this.region.Major?.ToString() ?? "-";
                    this.Minor = this.region.Minor?.ToString() ?? "-";
                    this.IsBusy = true;
                }
                catch (Exception ex)
                {
                    await this.Dialogs.Alert(ex.ToString());
                }
            }
        }


        public override void OnDisappearing()
        {
            this.IsBusy = false;
            this.scanner.Stop();
        }


        public ICommand SetRegion { get; }
        [Reactive] public string Uuid { get; private set; }
        [Reactive] public string Major { get; private set; }
        [Reactive] public string Minor { get; private set; }
        public ObservableCollection<ManagedBeacon> Beacons => this.scanner.Beacons;
    }
}
