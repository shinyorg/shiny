namespace Sample.Shared.Maui.Pages;

[ShellMap<MainPage>(registerRoute: false)]
public partial class MainViewModel(INavigator navigator) : ObservableObject
{
    public List<FeatureItem> Features { get; } = BuildFeatureList();

    static List<FeatureItem> BuildFeatureList()
    {
        // On macOS (Sample.MacOS uses Platform.Maui.MacOS — an AppKit host), only a
        // limited subset of Shiny services are actually functional today. Most others
        // either aren't registered (HTTP Transfers) or the underlying platform bits
        // don't surface through this MAUI host yet. Keep the menu honest.
        if (OperatingSystem.IsMacOS())
        {
            return new List<FeatureItem>
            {
                new("📡 BLE Scanner", "Scan for nearby Bluetooth LE devices", "blescan"),
                new("🔗 BLE L2CAP", "L2CAP CoC host & client demo", "blel2cap"),
                new("📝 Events", "Captured delegate events (SQLite)", "events"),
                new("🔋 Battery", "Observe battery level & state", "battery"),
                new("🌐 Connectivity", "Observe network connectivity", "connectivity")
            };
        }

        var list = new List<FeatureItem>
        {
            new("📡 BLE Scanner", "Scan for nearby Bluetooth LE devices", "blescan"),
            new("📢 BLE Hosting", "Advertise as a GATT server", "blehosting"),
            new("🔔 Notifications", "Local notifications", "notifications"),
            new("📣 Notification Channels", "Manage notification channels", "notificationchannels"),
            new("⬇️ HTTP Transfers", "Background uploads & downloads", "httptransfers"),
            new("📝 Events", "Captured delegate events (SQLite)", "events"),
            new("🔋 Battery", "Observe battery level & state", "battery"),
            new("🌐 Connectivity", "Observe network connectivity", "connectivity"),
            new("⚙️ Settings", "Connectivity, battery, key-value store", "settings")
        };

        // L2CAP: implemented on Android (29+), Apple (iOS/MacCatalyst/MacOS), and Linux/BlueZ.
        // Windows has no Bluetooth.GenericAttributeProfile L2CAP surface in WinRT.
        if (!OperatingSystem.IsWindows())
            list.Add(new("🔗 BLE L2CAP", "L2CAP CoC host & client demo", "blel2cap"));

        // Push: every MAUI-supported OS except Linux (no Shiny.Push Linux impl)
        if (!OperatingSystem.IsLinux())
            list.Add(new("📲 Push", "Push notification registration", "push"));

        // GPS / Geofencing: mobile-only (Shiny.Locations is wired for Android & iOS)
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            list.Add(new("📍 GPS", "Track location with GPS", "gps"));
            list.Add(new("🔲 Geofencing", "Monitor geofence regions", "geofencing"));
            list.Add(new("🏃 Motion Activity", "Activity recognition (walk, drive, etc.)", "motionactivity"));
        }

        // Jobs: Android, iOS, and Linux (not registered on the MacOS/Windows samples)
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsLinux())
            list.Add(new("⏰ Jobs", "Background job scheduling", "jobs"));

        return list;
    }

    [RelayCommand]
    Task Navigate(string route) => navigator.NavigateTo(route);
}

public record FeatureItem(string Title, string Description, string Route);
