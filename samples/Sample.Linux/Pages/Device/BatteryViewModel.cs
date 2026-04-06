namespace Sample.Linux.Pages.Device;

[ShellMap<BatteryPage>("battery")]
public partial class BatteryViewModel(Shiny.Power.IBattery battery) : ObservableObject, IDisposable
{
    IDisposable? sub;

    [ObservableProperty] string level = "—";
    [ObservableProperty] string status = "—";
    [ObservableProperty] string lastChange = "—";


    [RelayCommand]
    void Refresh()
    {
        this.Level = $"{battery.Level * 100:F0}%";
        this.Status = battery.Status.ToString();
    }


    [RelayCommand]
    void Start()
    {
        this.sub?.Dispose();
        this.Refresh();
        this.sub = battery
            .WhenChanged()
            .Subscribe(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.Refresh();
                    this.LastChange = DateTime.Now.ToString("HH:mm:ss");
                });
            });
    }


    [RelayCommand]
    void Stop()
    {
        this.sub?.Dispose();
        this.sub = null;
    }


    public void Dispose() => this.sub?.Dispose();
}
