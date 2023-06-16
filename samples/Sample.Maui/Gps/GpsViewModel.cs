using System.Reactive.Disposables;
using Shiny.Locations;

namespace Sample.Gps;


public class GpsViewModel : ViewModel
{
    readonly IGpsManager manager;


    public GpsViewModel(BaseServices services, IGpsManager manager) : base(services)
    {
        this.manager = manager;

        var l = this.manager.CurrentListener;
        this.IsUpdating = l != null;

        var mode = l?.BackgroundMode ?? GpsBackgroundMode.None;
        this.UseBackground = mode != GpsBackgroundMode.None;
        this.UseRealtime = mode == GpsBackgroundMode.Realtime;
        this.SelectedAccuracy = (l?.Accuracy ?? GpsAccuracy.Normal).ToString();

        this.GetCurrentPosition = this.CreateOneReading(LocationRetrieve.Current);
        this.GetLastReading = this.CreateOneReading(LocationRetrieve.Last);
        this.GetLastOrCurrent = this.CreateOneReading(LocationRetrieve.LastOrCurrent);
        this.Accuracies = new[]
        {
            GpsAccuracy.Highest.ToString(),
            GpsAccuracy.High.ToString(),
            GpsAccuracy.Normal.ToString(),
            GpsAccuracy.Low.ToString(),
            GpsAccuracy.Lowest.ToString()
        };

        this.ToggleUpdates = new Command(
            async () =>
            {
                if (this.manager.CurrentListener != null)
                {
                    await this.manager.StopListener();
                }
                else
                {
                    var access = await this.manager.RequestAccess(new GpsRequest
                    {
                        BackgroundMode = this.GetMode()
                    });
                    this.Access = access.ToString();

                    if (access != AccessState.Available)
                    {
                        await this.Dialogs.DisplayAlertAsync("ERROR", "Insufficient permissions - " + access, "OK");
                        return;
                    }

                    var accuracy = (GpsAccuracy)Enum.Parse(typeof(GpsAccuracy), this.SelectedAccuracy);
                    var request = new GpsRequest
                    {
                        BackgroundMode = this.GetMode(),
                        Accuracy = accuracy
                    };
                    try
                    {
                        await this.manager.StartListener(request);
                    }
                    catch (Exception ex)
                    {
                        await this.Dialogs.DisplayAlertAsync("ERROR", ex.ToString(), "OK");
                    }
                }
                this.IsUpdating = this.manager.CurrentListener != null;
            }
        );

        this.RequestAccess = new Command(async () =>
        {
            var request = new GpsRequest { BackgroundMode = this.GetMode() };
            this.Access = (await this.manager.RequestAccess(request)).ToString();
        });
    }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.manager
            .WhenReading()
            .SubOnMainThread(this.SetValues)
            .DisposedBy(this.DestroyWith);

        this.WhenAnyProperty(x => x.IsUpdating)
            .Select(x => x ? "Stop Listening" : "Start Updating")
            .Subscribe(x => this.ListenerText = x)
            .DisposedBy(this.DestroyWith);

        this.WhenAnyProperty()
            .Skip(1)
            .Subscribe(_ => this.ToggleUpdates.ChangeCanExecute())
            .DisposedBy(this.DestroyWith);

        return base.InitializeAsync(parameters);
    }


    public Command SelectAccuracy { get; }
    public Command GetLastReading { get; }
    public Command GetCurrentPosition { get; }
    public Command GetLastOrCurrent { get; }
    public Command RequestAccess { get; }
    public Command ToggleUpdates { get; }

    public string[] Accuracies { get; }
    [Reactive] public string SelectedAccuracy { get; set; }
    [Reactive] public string ListenerText { get; private set; }
    [Reactive] public string NotificationTitle { get; set; }
    [Reactive] public string NotificationMessage { get; set; }
    [Reactive] public bool UseBackground { get; set; }
    [Reactive] public bool UseRealtime { get; set; }
    [Reactive] public string Access { get; private set; }
    [Reactive] public bool IsUpdating { get; private set; }

    [Reactive] public double Latitude { get; private set; }
    [Reactive] public double Longitude { get; private set; }
    [Reactive] public double Altitude { get; private set; }
    [Reactive] public double PositionAccuracy { get; private set; }
    [Reactive] public double Heading { get; private set; }
    [Reactive] public double HeadingAccuracy { get; private set; }
    [Reactive] public double Speed { get; private set; }
    [Reactive] public DateTimeOffset Timestamp { get; private set; }


    void SetValues(GpsReading reading)
    {
        this.Latitude = reading.Position.Latitude;
        this.Longitude = reading.Position.Longitude;
        this.Altitude = reading.Altitude;
        this.PositionAccuracy = reading.PositionAccuracy;

        this.Heading = reading.Heading;
        this.HeadingAccuracy = reading.HeadingAccuracy;
        this.Speed = reading.Speed;
        this.Timestamp = reading.Timestamp;
    }


    GpsBackgroundMode GetMode()
    {
        var mode = GpsBackgroundMode.None;
        if (this.UseBackground)
        {
            mode = this.UseRealtime
                ? GpsBackgroundMode.Realtime
                : GpsBackgroundMode.Standard;
        }
        return mode;
    }


    Command CreateOneReading(LocationRetrieve retrieve) => new Command(async () =>
    {
        var observable = retrieve switch
        {
            LocationRetrieve.Last => this.manager.GetLastReading(),
            LocationRetrieve.Current => this.manager.GetCurrentPosition(),
            _ => this.manager.GetLastReadingOrCurrentPosition()
        };
        var reading = await observable.ToTask();

        if (reading == null)
            await this.Dialogs.DisplayAlertAsync("ERROR", "Could not getting GPS coordinates", "OK");
        else
            this.SetValues(reading);

        try
        {
            await this.manager.GetLastReadingOrCurrentPosition().ToTask();
            await Task.Delay(2000).ConfigureAwait(false);
            await this.manager.GetLastReadingOrCurrentPosition().ToTask();
            Console.WriteLine("success");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    });


    enum LocationRetrieve
    {
        Last,
        Current,
        LastOrCurrent
    }
}
