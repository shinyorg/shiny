using Shiny.Locations;

namespace Sample.Geofencing;


public class ListViewModel : ViewModel
{
    public ListViewModel(BaseServices services, IGeofenceManager geofenceManager) : base(services)
    {
        this.Create = this.Navigation.Command("GeofencingCreate");

        this.DropAllFences = this.ConfirmCommand(
            "Are you sure you wish to drop all geofences?",
            async () =>
            {
                await geofenceManager.StopAllMonitoring();
                this.Load!.Execute(null);
            }
        );

        this.Load = this.LoadingCommand(() =>
        {
            var geofences = geofenceManager.GetMonitorRegions();

            this.Geofences = geofences
                .Select(region => new GeofenceRegionViewModel
                (
                    region,
                    this.ConfirmCommand
                    (
                        "Are you sure you wish to remove geofence - " + region.Identifier,
                        async () =>
                        {
                            await geofenceManager.StopMonitoring(region.Identifier);
                            this.Load!.Execute(null);
                        }
                    ),
                    this.LoadingCommand(async () =>
                    {
                        var status = await geofenceManager.RequestState(region);
                        await this.Alert($"{region.Identifier} status is {status}");
                    })
                ))
                .ToList();

            this.RaisePropertyChanged(nameof(this.Geofences));
        });
    }


    public ICommand Create { get; }
    public ICommand Load { get; }
    public ICommand DropAllFences { get; }
    [Reactive] public IList<GeofenceRegionViewModel> Geofences { get; private set; }

    public override void OnNavigatedTo(INavigationParameters parameters) => this.Load.Execute(null);
}
