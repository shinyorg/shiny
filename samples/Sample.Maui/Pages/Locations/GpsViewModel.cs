namespace Sample.Maui.Pages.Locations;

[ShellMap<GpsPage>("gps")]
public partial class GpsViewModel(IGpsManager gpsManager) : ObservableObject, IDisposable
{
    IDisposable? _gpsSub;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ListenText))]
    bool isListening;

    [ObservableProperty] double latitude;
    [ObservableProperty] double longitude;
    [ObservableProperty] double altitude;
    [ObservableProperty] double speed;
    [ObservableProperty] double heading;

    public string ListenText => IsListening ? "Stop Listener" : "Start Listener";

    [RelayCommand]
    async Task ToggleListener()
    {
        if (IsListening)
        {
            _gpsSub?.Dispose();
            _gpsSub = null;
            await gpsManager.StopListener();
            IsListening = false;
            return;
        }

        var request = new GpsRequest(GpsBackgroundMode.Standard);
        var access = await gpsManager.RequestAccess(request);
        if (access != AccessState.Available)
        {
            await App.Current!.Windows[0].Page!.DisplayAlert("Error", $"GPS access is {access}", "OK");
            return;
        }

        await gpsManager.StartListener(request);
        IsListening = true;
        _gpsSub = gpsManager
            .WhenReading()
            .Subscribe(reading => MainThread.BeginInvokeOnMainThread(() =>
            {
                Latitude = reading.Position.Latitude;
                Longitude = reading.Position.Longitude;
                Altitude = reading.Altitude;
                Speed = reading.Speed;
                Heading = reading.Heading;
            }));
    }

    public void Dispose()
    {
        _gpsSub?.Dispose();
    }
}
