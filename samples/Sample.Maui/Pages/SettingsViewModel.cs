using Shiny.Net;
using Shiny.Power;

namespace Sample.Maui.Pages;

[ShellMap<SettingsPage>("settings")]
public partial class SettingsViewModel(Shiny.Net.IConnectivity connectivity, Shiny.Power.IBattery battery, IKeyValueStore store) : ObservableObject, IPageLifecycleAware, IDisposable
{
    IDisposable? _connSub;
    IDisposable? _batterySub;

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
        UpdateConnectivity();
        UpdateBattery();
        Platform = $"{Microsoft.Maui.Devices.DeviceInfo.Platform} {Microsoft.Maui.Devices.DeviceInfo.VersionString} ({Microsoft.Maui.Devices.DeviceInfo.Manufacturer} {Microsoft.Maui.Devices.DeviceInfo.Model})";

        _connSub = connectivity
            .WhenChanged()
            .Subscribe(_ => MainThread.BeginInvokeOnMainThread(UpdateConnectivity));

        _batterySub = battery
            .WhenChanged()
            .Subscribe(_ => MainThread.BeginInvokeOnMainThread(UpdateBattery));
    }

    public void OnDisappearing()
    {
        _connSub?.Dispose();
        _connSub = null;
        _batterySub?.Dispose();
        _batterySub = null;
    }

    void UpdateConnectivity()
    {
        NetworkAccess = connectivity.Access.ToString();
        ConnectionTypes = connectivity.ConnectionTypes.ToString();
    }

    void UpdateBattery()
    {
        BatteryLevel = battery.Level;
        BatteryState = battery.Status.ToString();
    }

    [RelayCommand]
    void SetValue()
    {
        if (string.IsNullOrWhiteSpace(StoreKey)) return;
        store.Set(StoreKey, StoreValue);
        StoreResult = $"Set '{StoreKey}' = '{StoreValue}'";
    }

    [RelayCommand]
    void GetValue()
    {
        if (string.IsNullOrWhiteSpace(StoreKey)) return;
        var value = store.Get(typeof(string), StoreKey);
        StoreResult = value != null ? $"'{StoreKey}' = '{value}'" : $"'{StoreKey}' not found";
    }

    [RelayCommand]
    void RemoveValue()
    {
        if (string.IsNullOrWhiteSpace(StoreKey)) return;
        var removed = store.Remove(StoreKey);
        StoreResult = removed ? $"Removed '{StoreKey}'" : $"'{StoreKey}' not found";
    }

    public void Dispose()
    {
        _connSub?.Dispose();
        _batterySub?.Dispose();
    }
}
