namespace Sample.Shared.Maui.Pages.Locations;

[ShellMap<GpsPage>("gps")]
public partial class GpsViewModel : ObservableObject, IDisposable, IPageLifecycleAware
{
    readonly IGpsManager gpsManager;
    readonly GpsDelegate? filter;
    EventHandler<GpsReading>? gpsHandler;

    public GpsViewModel(IGpsManager gpsManager, IEnumerable<IGpsDelegate> gpsDelegates)
    {
        this.gpsManager = gpsManager;
        this.filter = gpsDelegates.OfType<GpsDelegate>().FirstOrDefault();
    }

    // Configuration
    public List<string> BackgroundModes { get; } = ["Foreground", "Standard", "Realtime"];
    [ObservableProperty] int selectedModeIndex;
    [ObservableProperty] bool requestPreciseAccuracy;

    // Reading filters (delegate-level min/max thresholds; blank disables the filter)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterStatus))]
    string minDistanceMeters = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterStatus))]
    string minTimeSeconds = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterStatus))]
    string maxDistanceMeters = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterStatus))]
    string maxTimeSeconds = string.Empty;

    public string FilterStatus { get; private set; } = string.Empty;

    partial void OnMinDistanceMetersChanged(string value) => this.ApplyFilters();
    partial void OnMinTimeSecondsChanged(string value) => this.ApplyFilters();
    partial void OnMaxDistanceMetersChanged(string value) => this.ApplyFilters();
    partial void OnMaxTimeSecondsChanged(string value) => this.ApplyFilters();

    void ApplyFilters()
    {
        if (this.filter is null)
        {
            this.FilterStatus = "GpsDelegate filter not available on this platform";
            this.OnPropertyChanged(nameof(this.FilterStatus));
            return;
        }

        var errors = new List<string>();

        var minDist = ParseMeters(this.MinDistanceMeters, "Min distance", errors);
        var maxDist = ParseMeters(this.MaxDistanceMeters, "Max distance", errors);
        var minTime = ParseSeconds(this.MinTimeSeconds, "Min time", errors);
        var maxTime = ParseSeconds(this.MaxTimeSeconds, "Max time", errors);

        if (minDist is not null && maxDist is not null && minDist.TotalMeters > maxDist.TotalMeters)
            errors.Add("Min distance must be <= max distance");

        if (minTime is not null && maxTime is not null && minTime > maxTime)
            errors.Add("Min time must be <= max time");

        if (errors.Count > 0)
        {
            this.FilterStatus = string.Join("; ", errors);
        }
        else
        {
            this.filter.MinimumDistance = minDist;
            this.filter.MaximumDistance = maxDist;
            this.filter.MinimumTime = minTime;
            this.filter.MaximumTime = maxTime;
            this.FilterStatus = "Filters applied";
        }
        this.OnPropertyChanged(nameof(this.FilterStatus));
    }

    static Distance? ParseMeters(string raw, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!double.TryParse(raw, out var meters) || meters < 0)
        {
            errors.Add($"{label} must be a non-negative number");
            return null;
        }
        return Distance.FromMeters(meters);
    }

    static TimeSpan? ParseSeconds(string raw, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!double.TryParse(raw, out var seconds) || seconds < 0)
        {
            errors.Add($"{label} must be a non-negative number");
            return null;
        }
        return TimeSpan.FromSeconds(seconds);
    }

    public void OnAppearing()
    {
        if (this.filter is null) return;
        this.MinDistanceMeters = this.filter.MinimumDistance?.TotalMeters.ToString("0.##") ?? string.Empty;
        this.MaxDistanceMeters = this.filter.MaximumDistance?.TotalMeters.ToString("0.##") ?? string.Empty;
        this.MinTimeSeconds = this.filter.MinimumTime?.TotalSeconds.ToString("0.##") ?? string.Empty;
        this.MaxTimeSeconds = this.filter.MaximumTime?.TotalSeconds.ToString("0.##") ?? string.Empty;
    }

    public void OnDisappearing() { }

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
                this.gpsManager.GpsReadingReceived -= this.gpsHandler;
                this.gpsHandler = null;
            }
            await this.gpsManager.StopListener();
            this.IsListening = false;
            this.Status = "Listener stopped";
            return;
        }

        var request = this.BuildRequest();
        var access = await this.gpsManager.RequestAccess(request);
        if (access != AccessState.Available)
        {
            this.Status = $"Access: {access}";
            return;
        }

        await this.gpsManager.StartListener(request);
        this.IsListening = true;
        this.Status = "Listening...";
        this.gpsHandler = (_, reading) => MainThread.BeginInvokeOnMainThread(() => this.SetReading(reading));
        this.gpsManager.GpsReadingReceived += this.gpsHandler;
    }

    [RelayCommand]
    async Task GetCurrentPosition()
    {
        try
        {
            this.Status = "Getting position...";
            var reading = await this.gpsManager.GetCurrentPosition();
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
        var state = this.gpsManager.GetCurrentStatus(request);
        this.Status = $"Permission: {state}";
    }

    public void Dispose()
    {
        if (this.gpsHandler != null)
        {
            this.gpsManager.GpsReadingReceived -= this.gpsHandler;
            this.gpsHandler = null;
        }
    }
}
