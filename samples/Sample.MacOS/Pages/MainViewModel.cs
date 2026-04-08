namespace Sample.MacOS.Pages;

[ShellMap<MainPage>(registerRoute: false)]
public partial class MainViewModel(INavigator navigator) : ObservableObject
{
    public List<FeatureItem> Features { get; } =
    [
        new("BLE Scanner", "Scan for nearby Bluetooth LE devices", "blescan"),
        new("BLE Hosting", "Advertise a GATT server from this Mac", "blehosting"),
        new("Push Notifications", "Register for and receive APNs push", "push"),
        new("Local Notifications", "Schedule and send local notifications", "notifications"),
        new("Battery", "Monitor battery level and charging state", "battery"),
        new("Connectivity", "Monitor network connectivity", "connectivity"),
        new("HTTP Transfers", "Resumable background HTTP downloads and uploads", "httptransfers")
    ];

    [RelayCommand]
    Task Navigate(string route) => navigator.NavigateTo(route);
}

public record FeatureItem(string Title, string Description, string Route);
