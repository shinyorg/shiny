namespace Sample.Shared.Maui.Pages.Locations;

[ShellMap<GpsPage>("gps")]
public partial class GpsViewModel(IGpsManager gpsManager) : ObservableObject, IDisposable
{
    EventHandler<GpsReading>? gpsHandler;

    // Configuration
    public List<string> BackgroundModes { get; } = ["Foreground", "Standard", "Realtime"];
    [ObservableProperty] int selectedModeIndex;
    [ObservableProperty] bool requestPreciseAccuracy;

    // iOS options (always defined so XAML compiled bindings resolve on every platform;
    // visibility is gated at runtime with IsIos / IsAndroid)
    public List<string> ActivityTypes { get; } = ["Other", "Fitness", "Airborne", "Automotive Nav", "Other Nav"];
    [ObservableProperty] int selectedActivityTypeIndex;
    [ObservableProperty] bool showsBackgroundLocationIndicator = true;
    [ObservableProperty] bool pausesLocationUpdatesAutomatically;

    // Android options
    public List<string> GpsPriorities { get; } = ["Balanced", "High Accuracy", "Low Power", "Passive"];
    [ObservableProperty] int selectedGpsPriorityIndex;
    [ObservableProperty] bool waitForAccurateLocation;
    [ObservableProperty] bool stopForegroundServiceWithTask;

    public bool IsIos => Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.iOS;
    public bool IsAndroid => Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.Android;

    // State
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ListenText))]
    bool isListening;

    [ObservableProperty] string status = string.Empty;

    // Reading
    [ObservableProperty] double latitude;
    [ObservableProperty] double longitude;
    [ObservableProperty] double altitude;
    [ObservableProperty] double speed;
    [ObservableProperty] double heading;
    [ObservableProperty] double positionAccuracy;
    [ObservableProperty] double headingAccuracy;
    [ObservableProperty] double speedAccuracy;
    [ObservableProperty] string timestamp = string.Empty;
    [ObservableProperty] int floor;
    [ObservableProperty] bool isStationary;

    public string ListenText => this.IsListening ? "Stop Listener" : "Start Listener";

    GpsRequest BuildRequest()
    {
        var mode = this.SelectedModeIndex switch
        {
            1 => GpsBackgroundMode.Standard,
            2 => GpsBackgroundMode.Realtime,
            _ => GpsBackgroundMode.None
        };

#if IOS
        var activityType = this.SelectedActivityTypeIndex switch
        {
            1 => CoreLocation.CLActivityType.Fitness,
            2 => CoreLocation.CLActivityType.Airborne,
            3 => CoreLocation.CLActivityType.AutomotiveNavigation,
            4 => CoreLocation.CLActivityType.OtherNavigation,
            _ => CoreLocation.CLActivityType.Other
        };
        return new AppleGpsRequest(
            BackgroundMode: mode,
            ShowsBackgroundLocationIndicator: this.ShowsBackgroundLocationIndicator,
            PausesLocationUpdatesAutomatically: this.PausesLocationUpdatesAutomatically,
            ActivityType: activityType
        );
#elif ANDROID
        var priority = this.SelectedGpsPriorityIndex switch
        {
            1 => GpsPriority.HighAccuracy,
            2 => GpsPriority.LowPower,
            3 => GpsPriority.Passive,
            _ => GpsPriority.Balanced
        };
        return new AndroidGpsRequest(
            BackgroundMode: mode,
            GpsPriority: priority,
            WaitForAccurateLocation: this.WaitForAccurateLocation,
            StopForegroundServiceWithTask: this.StopForegroundServiceWithTask,
            RequestPreciseAccuracy: this.RequestPreciseAccuracy
        );
#else
        return new GpsRequest(mode, this.RequestPreciseAccuracy);
#endif
    }

    void SetReading(GpsReading reading)
    {
        this.Latitude = reading.Position.Latitude;
        this.Longitude = reading.Position.Longitude;
        this.Altitude = reading.Altitude;
        this.Speed = reading.Speed;
        this.Heading = reading.Heading;
        this.PositionAccuracy = reading.PositionAccuracy;
        this.HeadingAccuracy = reading.HeadingAccuracy;
        this.SpeedAccuracy = reading.SpeedAccuracy;
        this.Timestamp = reading.Timestamp.LocalDateTime.ToString("G");
        this.Floor = reading.Floor;
        this.IsStationary = reading.IsStationary;
    }

    [RelayCommand]
    async Task ToggleListener()
    {
        if (this.IsListening)
        {
            if (this.gpsHandler != null)
            {
                gpsManager.GpsReadingReceived -= this.gpsHandler;
                this.gpsHandler = null;
            }
            await gpsManager.StopListener();
            this.IsListening = false;
            this.Status = "Listener stopped";
            return;
        }

        var request = this.BuildRequest();
        var access = await gpsManager.RequestAccess(request);
        if (access != AccessState.Available)
        {
            this.Status = $"Access: {access}";
            return;
        }

        await gpsManager.StartListener(request);
        this.IsListening = true;
        this.Status = "Listening...";
        this.gpsHandler = (_, reading) => MainThread.BeginInvokeOnMainThread(() => this.SetReading(reading));
        gpsManager.GpsReadingReceived += this.gpsHandler;
    }

    [RelayCommand]
    async Task GetCurrentPosition()
    {
        try
        {
            this.Status = "Getting position...";
            var reading = await gpsManager.GetCurrentPosition();
            if (reading != null)
                this.SetReading(reading);
            this.Status = "Position received";
        }
        catch (Exception ex)
        {
            this.Status = "Error: " + ex;
        }
    }

    [RelayCommand]
    void CheckPermission()
    {
        var request = this.BuildRequest();
        var state = gpsManager.GetCurrentStatus(request);
        this.Status = $"Permission: {state}";
    }

    public void Dispose()
    {
        if (this.gpsHandler != null)
        {
            gpsManager.GpsReadingReceived -= this.gpsHandler;
            this.gpsHandler = null;
        }
    }
}
