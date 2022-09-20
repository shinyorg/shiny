using Shiny;
using Shiny.Locations;

namespace Sample.Notifications.Create;


public class LocationViewModel : ViewModel
{
    public LocationViewModel(BaseServices services, IGpsManager gpsManager) : base(services)
    {
        this.Cancel =  ReactiveCommand.CreateFromTask(async () => await this.Navigation.GoBack());

        this.Use = ReactiveCommand.CreateFromTask(async () =>
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
            await this.Navigation.GoBack();
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

    [Reactive] public double Latitude { get; set; }
    [Reactive] public double Longitude { get; set; }
    [Reactive] public int Radius { get; set; } = 1000;
}
