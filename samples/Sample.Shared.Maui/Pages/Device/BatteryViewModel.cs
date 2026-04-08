using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Device;

[ShellMap<BatteryPage>("battery")]
public partial class BatteryViewModel(Shiny.Power.IBattery battery, IMainThread mainThread) : ObservableObject, IPageLifecycleAware
{
    IDisposable? sub;

    [ObservableProperty] string status = "Unknown";
    [ObservableProperty] double level;

    public ObservableCollection<BatteryHistoryEntry> History { get; } = new();

    public void OnAppearing()
    {
        // Seed with the current state so the page isn't blank until the first change
        this.Status = battery.Status.ToString();
        this.Level = battery.Level;
        this.AddHistory(battery.Status.ToString(), battery.Level);

        this.sub = battery
            .WhenChanged()
            .Subscribe(b => mainThread.InvokeOnMainThreadAsync(() =>
            {
                this.Status = b.Status.ToString();
                this.Level = b.Level;
                this.AddHistory(b.Status.ToString(), b.Level);
            }));
    }

    public void OnDisappearing()
    {
        this.sub?.Dispose();
        this.sub = null;
        this.History.Clear();
    }

    [RelayCommand]
    void ClearHistory() => this.History.Clear();

    void AddHistory(string status, double level)
    {
        this.History.Insert(0, new BatteryHistoryEntry(DateTime.Now, status, level));
        while (this.History.Count > 100)
            this.History.RemoveAt(this.History.Count - 1);
    }
}

public record BatteryHistoryEntry(DateTime Timestamp, string Status, double Level);
