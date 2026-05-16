using Shiny.Net;
using Shiny.Power;

namespace Sample.Shared.Maui.Pages.Settings;

[ShellMap<SettingsPage>("settings")]
public partial class SettingsViewModel(Shiny.Net.IConnectivity connectivity, Shiny.Power.IBattery battery, IKeyValueStore store) : ObservableObject, IPageLifecycleAware, IDisposable
{
    IDisposable? connSub;
    IDisposable? batterySub;

    [ObservableProperty] string networkAccess = string.Empty;
    [ObservableProperty] string connectionTypes = string.Empty;
    [ObservableProperty] double batteryLevel;
    [ObservableProperty] string batteryState = string.Empty;
    [ObservableProperty] string platform = string.Empty;
    [ObservableProperty] string storeKey = string.Empty;
    [ObservableProperty] string storeValue = string.Empty;
    [ObservableProperty] string storeResult = string.Empty;

    public void OnAppearing()
    {
        this.UpdateConnectivity();
        this.UpdateBattery();
        this.Platform = $"{Microsoft.Maui.Devices.DeviceInfo.Platform} {Microsoft.Maui.Devices.DeviceInfo.VersionString} ({Microsoft.Maui.Devices.DeviceInfo.Manufacturer} {Microsoft.Maui.Devices.DeviceInfo.Model})";

        this.connSub = connectivity
            .WhenChanged()
            .Subscribe(_ => MainThread.BeginInvokeOnMainThread(this.UpdateConnectivity));

        this.batterySub = battery
            .WhenChanged()
            .Subscribe(_ => MainThread.BeginInvokeOnMainThread(this.UpdateBattery));
    }

    public void OnDisappearing()
    {
        this.connSub?.Dispose();
        this.connSub = null;
        this.batterySub?.Dispose();
        this.batterySub = null;
    }

    void UpdateConnectivity()
    {
        this.NetworkAccess = connectivity.Access.ToString();
        this.ConnectionTypes = connectivity.ConnectionTypes.ToString();
    }

    void UpdateBattery()
    {
        this.BatteryLevel = battery.Level;
        this.BatteryState = battery.Status.ToString();
    }

    [RelayCommand]
    void SetValue()
    {
        if (string.IsNullOrWhiteSpace(this.StoreKey)) return;
        store.Set(this.StoreKey, this.StoreValue);
        this.StoreResult = $"Set '{this.StoreKey}' = '{this.StoreValue}'";
    }

    [RelayCommand]
    void GetValue()
    {
        if (string.IsNullOrWhiteSpace(this.StoreKey)) return;
        var value = store.Get<string>(this.StoreKey);
        this.StoreResult = value != null ? $"'{this.StoreKey}' = '{value}'" : $"'{this.StoreKey}' not found";
    }

    [RelayCommand]
    void RemoveValue()
    {
        if (string.IsNullOrWhiteSpace(this.StoreKey)) return;
        var removed = store.Remove(this.StoreKey);
        this.StoreResult = removed ? $"Removed '{this.StoreKey}'" : $"'{this.StoreKey}' not found";
    }

    public void Dispose()
    {
        this.connSub?.Dispose();
        this.batterySub?.Dispose();
    }
}
