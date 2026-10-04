namespace Sample.Shared.Maui.Pages;

[ShellMap<MainPage>(registerRoute: false)]
public partial class MainViewModel(INavigator navigator) : ObservableObject
{
    static readonly ILookup<FeatureGroup, FeatureItem> Features = BuildFeatureList().ToLookup(x => x.Group);

    public FeatureSection Bluetooth { get; } = Section(FeatureGroup.Bluetooth, "Bluetooth & Beacons");
    public FeatureSection Notifications { get; } = Section(FeatureGroup.Notifications, "Notifications & Live Surfaces");
    public FeatureSection Background { get; } = Section(FeatureGroup.Background, "Background Work");
    public FeatureSection Location { get; } = Section(FeatureGroup.Location, "Location & Motion");
    public FeatureSection PersonalData { get; } = Section(FeatureGroup.PersonalData, "Personal Data");
    public FeatureSection Networking { get; } = Section(FeatureGroup.Networking, "Networking");
    public FeatureSection Peripherals { get; } = Section(FeatureGroup.Peripherals, "Devices & Peripherals");
    public FeatureSection Ai { get; } = Section(FeatureGroup.Ai, "AI & Assistants");
    public FeatureSection Device { get; } = Section(FeatureGroup.Device, "Device & Diagnostics");

    static FeatureSection Section(FeatureGroup group, string title) => new(title, Features[group].ToList());

    static List<FeatureItem> BuildFeatureList()
    {
        var list = new List<FeatureItem>();
        void Add(FeatureGroup group, string title, string description, string route)
            => list.Add(new(group, title, description, route));

        // On macOS (Sample.MacOS uses Platform.Maui.MacOS — an AppKit host), only a
        // limited subset of Shiny services are actually functional today. Most others
        // either aren't registered (HTTP Transfers) or the underlying platform bits
        // don't surface through this MAUI host yet. Keep the menu honest.
        if (OperatingSystem.IsMacOS())
        {
            Add(FeatureGroup.Bluetooth, "📡 BLE Scanner", "Scan for nearby Bluetooth LE devices", "blescan");
            Add(FeatureGroup.Bluetooth, "🔗 BLE L2CAP", "L2CAP CoC host & client demo", "blel2cap");
            // macOS ranges iBeacons and reads Eddystone, but CoreLocation has no beacon
            // region monitoring on the Mac - that page is left off deliberately.
            Add(FeatureGroup.Bluetooth, "🔵 Beacon Ranging", "Range iBeacons and estimate distance", "beaconranging");
            Add(FeatureGroup.Bluetooth, "🟢 Eddystone", "Scan Eddystone UID / URL / TLM frames", "eddystone");
            Add(FeatureGroup.Bluetooth, "📶 Beacon Broadcast", "Advertise as an iBeacon", "beaconbroadcast");
            Add(FeatureGroup.PersonalData, "📅 Calendar", "Browse & edit device calendar events", "calendar");
            Add(FeatureGroup.Peripherals, "🧾 Thermal Printer", "Print receipts to BLE & WiFi ESC/POS printers", "thermalprinter");
            Add(FeatureGroup.Peripherals, "🖨️ Native Print", "Print PDFs & HTML through CUPS", "nativeprint");
            Add(FeatureGroup.Ai, "🤖 AI Assistant", "Chat + Shiny AI tools via GitHub Copilot", "ai");
            Add(FeatureGroup.Device, "🔋 Battery", "Observe battery level & state", "battery");
            Add(FeatureGroup.Device, "🌐 Connectivity", "Observe network connectivity", "connectivity");
            Add(FeatureGroup.Device, "📝 Events", "Captured delegate events (SQLite)", "events");
            return list;
        }

        // --- Bluetooth & Beacons ---
        Add(FeatureGroup.Bluetooth, "📡 BLE Scanner", "Scan for nearby Bluetooth LE devices", "blescan");
        Add(FeatureGroup.Bluetooth, "📢 BLE Hosting", "Advertise as a GATT server", "blehosting");
        Add(FeatureGroup.Bluetooth, "🧬 BLE Hosting (Generated)", "Same GATT server via [BleService] attributes", "blehostinggen");
        // L2CAP: Android (29+), Apple, Linux/BlueZ. No WinRT surface for it.
        if (!OperatingSystem.IsWindows())
            Add(FeatureGroup.Bluetooth, "🔗 BLE L2CAP", "L2CAP CoC host & client demo", "blel2cap");
        Add(FeatureGroup.Bluetooth, "🔵 Beacon Ranging", "Range iBeacons and estimate distance", "beaconranging");
        Add(FeatureGroup.Bluetooth, "🎯 Beacon Monitoring", "Enter/exit beacon regions in the background", "beaconmonitoring");
        Add(FeatureGroup.Bluetooth, "🟢 Eddystone", "Scan Eddystone UID / URL / TLM frames", "eddystone");
        Add(FeatureGroup.Bluetooth, "📶 Beacon Broadcast", "Advertise as an iBeacon or Eddystone beacon", "beaconbroadcast");

        // --- Notifications & Push ---
        Add(FeatureGroup.Notifications, "🔔 Notifications", "Local notifications", "notifications");
        Add(FeatureGroup.Notifications, "📣 Notification Channels", "Manage notification channels", "notificationchannels");
        Add(FeatureGroup.Notifications, "⏳ Pending Notifications", "View & cancel scheduled notifications", "pendingnotifications");
        // Push: every MAUI-supported OS except Linux (no Shiny.Push Linux impl)
        if (!OperatingSystem.IsLinux())
            Add(FeatureGroup.Notifications, "📲 Push", "Push notification registration", "push");

        // --- Live Activities (iOS/iPadOS ActivityKit + Android 16 Live Updates; ActivityKit is unavailable on Mac Catalyst) ---
        if (OperatingSystem.IsAndroid() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()))
            Add(FeatureGroup.Notifications, "🏝️ Live Activities", "Lock Screen & Dynamic Island / Android live updates", "liveactivities");

        // --- Background work ---
        // Jobs (Android, iOS, Linux)
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsLinux())
            Add(FeatureGroup.Background, "⏰ Jobs", "Background job scheduling", "jobs");
        Add(FeatureGroup.Background, "⬇️ HTTP Transfers", "Background uploads & downloads", "httptransfers");

        // --- Location / Activity (mobile-only) ---
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            Add(FeatureGroup.Location, "📍 GPS", "Track location with GPS", "gps");
            Add(FeatureGroup.Location, "🔲 Geofencing", "Monitor geofence regions", "geofencing");
            Add(FeatureGroup.Location, "🏃 Motion Activity", "Activity recognition (walk, drive, etc.)", "motionactivity");
        }

        // --- Contacts (iOS, Mac Catalyst & Android; IsIOS() is also true on Mac Catalyst) ---
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
            Add(FeatureGroup.PersonalData, "👤 Contacts", "Browse, search & edit device contacts", "contacts");

        // --- Calendar (every MAUI-native platform) ---
        Add(FeatureGroup.PersonalData, "📅 Calendar", "Browse & edit device calendar events", "calendar");

        // --- Networking ---
        Add(FeatureGroup.Networking, "📶 Wi-Fi", "Scan, join & manage Wi-Fi networks", "wifi");
        Add(FeatureGroup.Networking, "🔎 mDNS / Bonjour", "Browse DNS-SD services on the local network", "mdns");
        Add(FeatureGroup.Networking, "🛰️ SSDP / UPnP", "Find routers, media servers & smart TVs", "ssdp");
        Add(FeatureGroup.Networking, "🎥 WS-Discovery", "Find ONVIF cameras, WSD printers & PCs", "wsdiscovery");

        // --- Devices & peripherals ---
        Add(FeatureGroup.Peripherals, "🧾 Thermal Printer", "Print receipts to BLE & WiFi ESC/POS printers", "thermalprinter");
        Add(FeatureGroup.Peripherals, "🖨️ Native Print", "Print PDFs & HTML through the OS print dialog", "nativeprint");
        Add(FeatureGroup.Peripherals, "🎬 Screen Recorder", "Record the screen to a video file", "screenrecorder");

        // --- AI Assistant (GitHub Copilot + Shiny *.Extensions.AI tools) ---
        Add(FeatureGroup.Ai, "🤖 AI Assistant", "Chat with your device via GitHub Copilot", "ai");

        // --- App Functions (Siri / App Intents on iOS, Gemini / AppFunctions on Android 16+; not Mac Catalyst) ---
        if (OperatingSystem.IsAndroid() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()))
            Add(FeatureGroup.Ai, "🗣️ App Functions", "Orders app driven by Siri & Gemini", "appfunctions");

        // --- Device state & diagnostics ---
        Add(FeatureGroup.Device, "🔋 Battery", "Observe battery level & state", "battery");
        Add(FeatureGroup.Device, "🌐 Connectivity", "Observe network connectivity", "connectivity");
        Add(FeatureGroup.Device, "⚙️ Settings", "Connectivity, battery, key-value store", "settings");
        Add(FeatureGroup.Device, "📝 Events", "Captured delegate events (SQLite)", "events");
        return list;
    }

    [RelayCommand]
    Task Navigate(string route) => navigator.NavigateTo(route);
}

public enum FeatureGroup
{
    Bluetooth,
    Notifications,
    Background,
    Location,
    PersonalData,
    Networking,
    Peripherals,
    Ai,
    Device
}

public record FeatureItem(FeatureGroup Group, string Title, string Description, string Route);

public record FeatureSection(string Title, IReadOnlyList<FeatureItem> Items)
{
    // a platform that has nothing in a group gets no empty header for it
    public bool IsVisible => this.Items.Count > 0;
}
