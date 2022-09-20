using Shiny;
using Shiny.Locations;
using Shiny.Notifications;

namespace Sample.Geofencing;


public class CreateViewModel : ViewModel
{
    public CreateViewModel(
        BaseServices services,
        IGeofenceManager geofenceManager,
        IGpsManager gpsManager,
        INotificationManager notificationManager
    ) : base(services)
    {
        this.SetCurrentLocation = new Command(async ct =>
        {
            var loc = await gpsManager.GetCurrentPosition().ToTask(ct);
            this.CenterLatitude = loc?.Position?.Latitude.ToString() ?? "";
            this.CenterLongitude = loc?.Position?.Longitude.ToString() ?? "";
        });

        this.RequestAccess = ReactiveCommand.CreateFromTask(async () =>
        {
            this.AccessStatus = await geofenceManager.RequestAccess();
        });

        this.SetCNTower = ReactiveCommand.Create(() =>
        {
            this.Identifier = "CNTowerToronto";
            this.CenterLatitude = "43.6425662";
            this.CenterLongitude = "-79.3892508";
        });

        this.SetAppleHQ = ReactiveCommand.Create(() =>
        {
            this.Identifier = "AppleHQ";
            this.CenterLatitude = "37.3320045";
            this.CenterLongitude = "-122.0329699";
        });

        this.CreateGeofence = ReactiveCommand.CreateFromTask(async () =>
        {

            var access = await geofenceManager.RequestAccess();

            if (access != AccessState.Available)
            {
                await this.Alert("Invalid Permission: " + access);
            }
            else
            {
                if (this.Identifier.IsEmpty())
                {
                    await this.Alert("Please enter an identifier");
                    return;
                }
                if (!this.NotifyOnEntry && !this.NotifyOnExit)
                {
                    await this.Alert("You must pick 1 notify option");
                    return;
                }
                if (this.RadiusMeters < 100)
                {
                    await this.Alert("Geofence Radius must be at least 100m");
                    return;
                }
                if (!Double.TryParse(this.CenterLatitude, out var lat) || (lat < -89.9 || lat > 89.9))
                {
                    await this.Alert("Invalid Latitude Value");
                    return;
                }
                if (!Double.TryParse(this.CenterLongitude, out var lng) && (lng < -179.9 || lng > 179.9))
                {
                    await this.Alert("Invalid Longitude Value");
                    return;
                }
                try
                {
                    access = await notificationManager.RequestAccess();
                    if (access != AccessState.Available)
                    {
                        await this.Alert("Permission denied to notifications - geofence will still be created and events will be stored, but you will not receive notifications");
                        return;
                    }

                    await geofenceManager.StartMonitoring(new GeofenceRegion(
                        this.Identifier,
                        new Position(
                            Double.Parse(this.CenterLatitude),
                            Double.Parse(this.CenterLongitude)
                        ),
                        Distance.FromMeters(this.RadiusMeters)
                    )
                    {
                        NotifyOnEntry = this.NotifyOnEntry,
                        NotifyOnExit = this.NotifyOnExit,
                        SingleUse = this.SingleUse
                    });
                    await this.Navigation.GoBack();
                }
                catch (Exception ex)
                {
                    await this.Alert("ERROR: " + ex);
                }
            }
        });
    }


    public ICommand RequestAccess { get; }
    public ICommand CreateGeofence { get; }
    public ICommand SetCurrentLocation { get; }
    public ICommand SetCNTower { get; }
    public ICommand SetAppleHQ { get; }


    [Reactive] public AccessState AccessStatus { get; private set; }
    [Reactive] public string Identifier { get; set; }
    [Reactive] public string CenterLatitude { get; set; }
    [Reactive] public string CenterLongitude { get; set; }
    [Reactive] public int RadiusMeters { get; set; } = 200;
    [Reactive] public bool SingleUse { get; set; }
    [Reactive] public bool NotifyOnEntry { get; set; } = true;
    [Reactive] public bool NotifyOnExit { get; set; } = true;
}
