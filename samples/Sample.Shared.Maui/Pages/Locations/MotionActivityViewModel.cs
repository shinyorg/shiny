namespace Sample.Shared.Maui.Pages.Locations;

[ShellMap<MotionActivityPage>("motionactivity")]
public partial class MotionActivityViewModel(IMotionActivityManager manager) : ObservableObject, IDisposable
{
    EventHandler<MotionActivityReading>? activityHandler;

    // State
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ListenText))]
    bool isListening;

    [ObservableProperty] string status = string.Empty;

    // Reading
    [ObservableProperty] string activity = string.Empty;
    [ObservableProperty] string confidence = string.Empty;
    [ObservableProperty] string timestamp = string.Empty;

    public string ListenText => this.IsListening ? "Stop Listener" : "Start Listener";

    void SetReading(MotionActivityReading reading)
    {
        this.Activity = reading.Activity.ToString();
        this.Confidence = reading.Confidence.ToString();
        this.Timestamp = reading.Timestamp.LocalDateTime.ToString("G");
    }

    [RelayCommand]
    async Task ToggleListener()
    {
        if (this.IsListening)
        {
            if (this.activityHandler != null)
            {
                manager.MotionActivityReadingReceived -= this.activityHandler;
                this.activityHandler = null;
            }
            await manager.StopListener();
            this.IsListening = false;
            this.Status = "Listener stopped";
            return;
        }

        var access = await manager.RequestAccess();
        if (access != AccessState.Available)
        {
            this.Status = $"Access: {access}";
            return;
        }

        await manager.StartListener();
        this.IsListening = true;
        this.Status = "Listening...";
        this.activityHandler = (_, reading) => MainThread.BeginInvokeOnMainThread(() => this.SetReading(reading));
        manager.MotionActivityReadingReceived += this.activityHandler;
    }

    [RelayCommand]
    async Task GetLastReading()
    {
        try
        {
            this.Status = "Getting last reading...";
            var reading = await manager.GetLastReading();
            if (reading == null)
            {
                this.Status = "No reading available";
            }
            else
            {
                this.SetReading(reading);
                this.Status = "Reading received";
            }
        }
        catch (Exception ex)
        {
            this.Status = "Error: " + ex.Message;
        }
    }

    [RelayCommand]
    void CheckPermission()
    {
        var state = manager.GetCurrentStatus();
        this.Status = $"Permission: {state}";
    }

    public void Dispose()
    {
        if (this.activityHandler != null)
        {
            manager.MotionActivityReadingReceived -= this.activityHandler;
            this.activityHandler = null;
        }
    }
}
