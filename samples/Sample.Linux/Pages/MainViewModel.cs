namespace Sample.Linux.Pages;

[ShellMap<MainPage>(registerRoute: false)]
public partial class MainViewModel(INavigator navigator) : ObservableObject
{
    public List<FeatureItem> Features { get; } =
    [
        new("BLE Scanner", "Scan for nearby Bluetooth LE devices", "blescan"),
        new("BLE Hosting", "Host a BLE GATT server and advertise it", "blehosting"),
        new("Battery", "View battery state via sysfs", "battery"),
        new("Connectivity", "View network state via NetworkInformation", "connectivity"),
        new("Notifications", "Send freedesktop desktop notifications", "notifications"),
        new("HTTP Transfers", "Resumable background HTTP downloads and uploads", "httptransfers")
    ];

    [RelayCommand]
    Task Navigate(string route) => navigator.NavigateTo(route);
}

public record FeatureItem(string Title, string Description, string Route);
