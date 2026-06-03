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
                new("🔋 Battery", "Observe battery level & state", "battery"),
                new("🌐 Connectivity", "Observe network connectivity", "connectivity"),
                new("📝 Events", "Captured delegate events (SQLite)", "events")
            };
        }

        var list = new List<FeatureItem>();

        // --- BLE ---
        list.Add(new("📡 BLE Scanner", "Scan for nearby Bluetooth LE devices", "blescan"));
        list.Add(new("📢 BLE Hosting", "Advertise as a GATT server", "blehosting"));
        // L2CAP: Android (29+), Apple, Linux/BlueZ. No WinRT surface for it.
        if (!OperatingSystem.IsWindows())
            list.Add(new("🔗 BLE L2CAP", "L2CAP CoC host & client demo", "blel2cap"));

        // --- Notifications & Push ---
        list.Add(new("🔔 Notifications", "Local notifications", "notifications"));
        list.Add(new("📣 Notification Channels", "Manage notification channels", "notificationchannels"));
        list.Add(new("⏳ Pending Notifications", "View & cancel scheduled notifications", "pendingnotifications"));
        // Push: every MAUI-supported OS except Linux (no Shiny.Push Linux impl)
        if (!OperatingSystem.IsLinux())
            list.Add(new("📲 Push", "Push notification registration", "push"));

        // --- Background transfers ---
        list.Add(new("⬇️ HTTP Transfers", "Background uploads & downloads", "httptransfers"));

        // --- Location / Activity (mobile-only) ---
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            list.Add(new("📍 GPS", "Track location with GPS", "gps"));
            list.Add(new("🔲 Geofencing", "Monitor geofence regions", "geofencing"));
            list.Add(new("🏃 Motion Activity", "Activity recognition (walk, drive, etc.)", "motionactivity"));
        }

        // --- Jobs (Android, iOS, Linux) ---
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsLinux())
            list.Add(new("⏰ Jobs", "Background job scheduling", "jobs"));

        // --- Device state ---
        list.Add(new("🔋 Battery", "Observe battery level & state", "battery"));
        list.Add(new("🌐 Connectivity", "Observe network connectivity", "connectivity"));
        list.Add(new("⚙️ Settings", "Connectivity, battery, key-value store", "settings"));

        // --- Diagnostics ---
        list.Add(new("📝 Events", "Captured delegate events (SQLite)", "events"));
        return list;
    }

    [RelayCommand]
    Task Navigate(string route) => navigator.NavigateTo(route);
}

public record FeatureItem(string Title, string Description, string Route);
