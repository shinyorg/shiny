namespace Sample.Maui.Pages;

[ShellMap<MainPage>(registerRoute: false)]
public partial class MainViewModel(INavigator navigator) : ObservableObject
{
    public List<FeatureItem> Features { get; } =
    [
        new("BLE Scanner", "Scan for nearby Bluetooth LE devices", "blescan"),
        new("BLE Hosting", "Advertise as a GATT server", "blehosting"),
        new("GPS", "Track location with GPS", "gps"),
        new("Geofencing", "Monitor geofence regions", "geofencing"),
        new("Notifications", "Local notifications", "notifications"),
        new("Push", "Push notification registration", "push"),
        new("HTTP Transfers", "Background file downloads", "httptransfers"),
        new("Jobs", "Background job scheduling", "jobs"),
        new("Settings", "Connectivity, battery, key-value store", "settings")
    ];

    [RelayCommand]
    Task Navigate(string route) => navigator.NavigateTo(route);
}

public record FeatureItem(string Title, string Description, string Route);
