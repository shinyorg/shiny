using Shiny.Power;

namespace Sample.MacOS.Pages.Battery;

[ShellMap<BatteryPage>("battery")]
public partial class BatteryViewModel(Shiny.Power.IBattery battery) : ObservableObject, IPageLifecycleAware
{
    IDisposable? sub;

    [ObservableProperty] string status = "Unknown";
    [ObservableProperty] double level;

    public void OnAppearing()
    {
        this.sub = battery
            .WhenChanged()
            .Subscribe(b => MainThread.BeginInvokeOnMainThread(() =>
            {
                this.Status = b.Status.ToString();
                this.Level = b.Level;
            }));
    }

    public void OnDisappearing() => this.sub?.Dispose();
}
