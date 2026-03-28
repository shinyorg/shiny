namespace Sample.Maui.Pages;

[ShellMap<MainPage>(registerRoute: false)]
public partial class MainViewModel(INavigator navigator) : ObservableObject
{
    public List<FeatureItem> Features { get; } = BuildFeatureList();

    static List<FeatureItem> BuildFeatureList()
    {
        var list = new List<FeatureItem>
        {
            new("BLE Scanner", "Scan for nearby Bluetooth LE devices", "blescan"),
        };

#if !WINDOWS
        list.Add(new("BLE Hosting", "Advertise as a GATT server", "blehosting"));
        list.Add(new("GPS", "Track location with GPS", "gps"));
        list.Add(new("Geofencing", "Monitor geofence regions", "geofencing"));
        list.Add(new("Notifications", "Local notifications", "notifications"));
        list.Add(new("Notification Channels", "Manage notification channels", "notificationchannels"));
        list.Add(new("Push", "Push notification registration", "push"));
        list.Add(new("HTTP Transfers", "Background uploads & downloads", "httptransfers"));
        list.Add(new("Jobs", "Background job scheduling", "jobs"));
#endif

        list.Add(new("Settings", "Connectivity, battery, key-value store", "settings"));
        return list;
    }

    [RelayCommand]
    Task Navigate(string route) => navigator.NavigateTo(route);
}

public record FeatureItem(string Title, string Description, string Route);
