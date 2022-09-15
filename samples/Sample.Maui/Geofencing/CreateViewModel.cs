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

        this.RequestAccess = new Command(async () =>
        {
            this.AccessStatus = await geofenceManager.RequestAccess();
        });

        this.SetCNTower = new Command(() =>
        {
            this.Identifier = "CNTowerToronto";
            this.CenterLatitude = "43.6425662";
            this.CenterLongitude = "-79.3892508";
        });

        this.SetAppleHQ = new Command(() =>
        {
            this.Identifier = "AppleHQ";
            this.CenterLatitude = "37.3320045";
            this.CenterLongitude = "-122.0329699";
        });

        this.CreateGeofence = new Command(async () =>
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
                    await this.Navigation.PopAsync();
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


    AccessState state;
    public AccessState AccessStatus
    {
        get => this.state;
        private set => this.Set(ref this.state, value);
    }


    string identifier;
    public string Identifier
    {
        get => this.identifier;
        set => this.Set(ref this.identifier, value);
    }


    string centerLat;
    public string CenterLatitude
    {
        get => this.centerLat;
        set => this.Set(ref this.centerLat, value);
    }


    string centerLong;
    public string CenterLongitude
    {
        get => this.centerLong;
        set => this.Set(ref this.centerLong, value);
    }


    int radius = 200;
    public int RadiusMeters
    {
        get => this.radius;
        set => this.Set(ref this.radius, value);
    }


    bool singleUse;
    public bool SingleUse
    {
        get => this.singleUse;
        set => this.Set(ref this.singleUse, value);
    }


    bool notifyEntry = true;
    public bool NotifyOnEntry
    {
        get => this.notifyEntry;
        set => this.Set(ref this.notifyEntry, value);
    }


    bool notifyExit = true;
    public bool NotifyOnExit
    {
        get => this.notifyExit;
        set => this.Set(ref this.notifyExit, value);
    }
}
