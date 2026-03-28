namespace Sample.Maui.Pages.Locations;

[ShellMap<GpsPage>("gps")]
public partial class GpsViewModel(IGpsManager gpsManager) : ObservableObject, IDisposable
{
    IDisposable? gpsSub;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ListenText))]
    bool isListening;

    [ObservableProperty] double latitude;
    [ObservableProperty] double longitude;
    [ObservableProperty] double altitude;
    [ObservableProperty] double speed;
    [ObservableProperty] double heading;

    public string ListenText => this.IsListening ? "Stop Listener" : "Start Listener";

    [RelayCommand]
    async Task ToggleListener()
    {
        if (this.IsListening)
        {
            this.gpsSub?.Dispose();
            this.gpsSub = null;
            await gpsManager.StopListener();
            this.IsListening = false;
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
        this.IsListening = true;
        this.gpsSub = gpsManager
            .WhenReading()
            .Subscribe(reading => MainThread.BeginInvokeOnMainThread(() =>
            {
                this.Latitude = reading.Position.Latitude;
                this.Longitude = reading.Position.Longitude;
                this.Altitude = reading.Altitude;
                this.Speed = reading.Speed;
                this.Heading = reading.Heading;
            }));
    }

    public void Dispose() => this.gpsSub?.Dispose();
}
