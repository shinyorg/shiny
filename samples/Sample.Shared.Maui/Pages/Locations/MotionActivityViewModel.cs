using System.Reactive.Threading.Tasks;

namespace Sample.Shared.Maui.Pages.Locations;

[ShellMap<MotionActivityPage>("motionactivity")]
public partial class MotionActivityViewModel(IMotionActivityManager manager) : ObservableObject, IDisposable
{
    IDisposable? activitySub;

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
            this.activitySub?.Dispose();
            this.activitySub = null;
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
        this.activitySub = manager
            .WhenReading()
            .Subscribe(reading => MainThread.BeginInvokeOnMainThread(() => this.SetReading(reading)));
    }

    [RelayCommand]
    async Task GetLastReading()
    {
        try
        {
            this.Status = "Getting last reading...";
            var reading = await manager.GetLastReading().ToTask();
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

    public void Dispose() => this.activitySub?.Dispose();
}
