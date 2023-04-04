using Shiny;
using Shiny.Beacons;

namespace Sample.Beacons;


public class MonitoringViewModel : ViewModel
{
    readonly IBeaconMonitoringManager? beaconManager;


    public MonitoringViewModel(BaseServices services, IBeaconMonitoringManager? beaconManager = null) : base(services)
    {
        this.beaconManager = beaconManager;

        this.Add = this.Navigation.Command(
            "BeaconCreate",
            p => p.Add("Monitoring", true)
        );
        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            if (this.beaconManager == null)
            {
                await this.Dialogs.DisplayAlertAsync("ERROR", "Beacon monitoring is not supported on this platform", "OK");
                return;
            }

            var regions = this.beaconManager
                .GetMonitoredRegions()
                .Select(x => new CommandItem
                {
                    Text = $"{x.Identifier}",
                    Detail = $"{x.Uuid}/{x.Major ?? 0}/{x.Minor ?? 0}",
                    PrimaryCommand = ReactiveCommand.Create(() =>
                    {
                        this.beaconManager.StopMonitoring(x.Identifier);
                        this.Load!.Execute(null);
                    })
                })
                .ToList();

            this.Regions = regions;
        });

        this.StopAllMonitoring = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var result = await this.Confirm("Are you sure you wish to stop all monitoring");
                if (result)
                {
                    this.beaconManager!.StopAllMonitoring();
                    this.Load.Execute(null);
                }
            },
            Observable.Return(this.beaconManager != null)
        );
    }


    public ICommand Load { get; }
    public ICommand Add { get; }
    public ICommand StopAllMonitoring { get; }
    [Reactive] public IList<CommandItem> Regions { get; private set; }


    public override async void OnNavigatedTo(INavigationParameters parameters)
    {
        if (parameters.IsBackNavigation())
        {
            var region = parameters.GetValue<BeaconRegion>(nameof(BeaconRegion));
            if (region != null && this.beaconManager != null)
            {
                var access = await this.beaconManager.RequestAccess();
                if (access == AccessState.Available)
                    await this.beaconManager.StartMonitoring(region);
                else
                    await this.Alert("Invalid Permissions: " + access);
            }
        }
        this.Load.Execute(null);
    }
}