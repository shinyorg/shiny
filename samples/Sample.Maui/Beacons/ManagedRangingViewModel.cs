using Shiny.Beacons;
using Shiny.Beacons.Managed;

namespace Sample.Beacons;


public class ManagedRangingViewModel : ViewModel
{
    readonly ManagedScan scanner;
    BeaconRegion? region;


    public ManagedRangingViewModel(BaseServices services, IBeaconRangingManager beaconManager) : base(services)
    {
        this.scanner = beaconManager.CreateManagedScan();
        this.SetRegion = this.Navigation.Command(
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
                await this.Dialogs.DisplayAlertAsync("ERROR", ex.ToString(), "OK");
            }
        }
    }


    //public override void OnDisappearing()
    //{
    //    this.IsBusy = false;
    //    this.scanner.Stop();
    //}


    public ICommand SetRegion { get; }
    [Reactive] public string Uuid { get; private set; }
    [Reactive] public string Major { get; private set; }
    [Reactive] public string Minor { get; private set; }
    public ObservableCollection<ManagedBeacon> Beacons => this.scanner.Beacons;
}
