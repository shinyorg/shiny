using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Device;

[ShellMap<BatteryPage>("battery")]
public partial class BatteryViewModel(Shiny.Power.IBattery battery, IMainThread mainThread) : ObservableObject, IPageLifecycleAware
{
    IDisposable? sub;

    [ObservableProperty] string status = "Unknown";
    [ObservableProperty] double level;

    public void OnAppearing()
    {
        this.sub = battery
            .WhenChanged()
            .Subscribe(b => mainThread.InvokeOnMainThreadAsync(() =>
            {
                this.Status = b.Status.ToString();
                this.Level = b.Level;
            }));
    }

    public void OnDisappearing() => this.sub?.Dispose();
}
