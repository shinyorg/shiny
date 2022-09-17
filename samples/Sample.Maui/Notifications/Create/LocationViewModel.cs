using Shiny;
using Shiny.Locations;

namespace Sample.Notifications.Create;


public class LocationViewModel : ViewModel
{
    public LocationViewModel(BaseServices services, IGpsManager gpsManager) : base(services)
    {
        this.Cancel =  new Command(async () => await this.Navigation.PopModalAsync());

        this.Use = new Command(async () =>
        {
            if (this.Radius < 150 || this.Radius > 5000)
            {
                await this.Alert("Radius must be between 150-5000 meters");
                return;
            }
            State.CurrentNotification!.RepeatInterval = null;
            State.CurrentNotification!.ScheduleDate = null;

            State.CurrentNotification!.Geofence = new Shiny.Notifications.GeofenceTrigger
            {
                Center = new Position(
                    this.Latitude,
                    this.Longitude
                ),
                Radius = Distance.FromMeters(this.Radius),
                Repeat = true
            };
            await this.Navigation.PopModalAsync();
        });

        this.SetCnTower = new Command(() =>
        {
            this.Latitude = 43.6425701;
            this.Longitude = -79.3892455;
        });

        this.SetCurrentLocation = this.LoadingCommand(async () =>
        { 
            var reading = await gpsManager
                .GetCurrentPosition()
                .Timeout(TimeSpan.FromSeconds(20))
                .ToTask();

            this.Latitude = reading?.Position?.Latitude ?? 0;
            this.Longitude = reading?.Position?.Longitude ?? 0;
        });
    }


    public ICommand Use { get; }
    public ICommand Cancel { get; }
    public ICommand SetCnTower { get; }
    public ICommand SetCurrentLocation { get; }


    double latitude;
    public double Latitude
    {
        get => this.latitude;
        set => this.Set(ref this.latitude, value);
    }


    double longitude;
    public double Longitude
    {
        get => this.longitude;
        set => this.Set(ref this.longitude, value);
    }


    int radius = 1000;
    public int Radius
    {
        get => this.radius;
        set => this.Set(ref this.radius, value);
    }
}
