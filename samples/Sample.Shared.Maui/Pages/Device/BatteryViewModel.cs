using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Device;

[ShellMap<BatteryPage>("battery")]
public partial class BatteryViewModel(Shiny.Power.IBattery battery, IMainThread mainThread) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = "Unknown";
    [ObservableProperty] double level;

    public ObservableCollection<BatteryHistoryEntry> History { get; } = new();

    public void OnAppearing()
    {
        this.Status = battery.Status.ToString();
        this.Level = battery.Level;
        this.AddHistory(battery.Status.ToString(), battery.Level);

        battery.Changed += this.OnChanged;
    }

    public void OnDisappearing()
    {
        battery.Changed -= this.OnChanged;
        this.History.Clear();
    }

    void OnChanged(object? sender, EventArgs e)
    {
        mainThread.InvokeOnMainThreadAsync(() =>
        {
            this.Status = battery.Status.ToString();
            this.Level = battery.Level;
            this.AddHistory(battery.Status.ToString(), battery.Level);
        });
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
