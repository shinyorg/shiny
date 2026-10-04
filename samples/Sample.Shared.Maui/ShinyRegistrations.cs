using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Media;
using Sample.Shared.Maui.Services;
using Sample.Shared.Maui.Ai;
using Shiny.Calendar.Extensions.AI;
using Shiny.Contacts.Extensions.AI;
using Shiny.Notifications.Extensions.AI;

namespace Sample.Shared.Maui;

public static class ShinyRegistrations
{
    public static MauiAppBuilder UseSampleShiny(this MauiAppBuilder builder)
    {
        builder
            .UseShinyControls()
            .UseShinyShell(x => x.AddGeneratedMaps())
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var s = builder.Services;
        s.AddSingleton<AppStateTracker>();
        s.AddSingleton<IEventStore, SqliteEventStore>();

#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        s.AddBattery();
        s.AddConnectivity();
#endif

#if IOS || ANDROID
        // Live Activities: ActivityKit on iOS (the widget extension is built by Sample.Maui - see
        // ShinyLiveActivityWidget there) and promoted ongoing notifications on Android.
        // Registered before AddTransferProgress, which otherwise registers its own manager.
        s.AddLiveActivities();
#endif

#if IOS || ANDROID || MACCATALYST || WINDOWS
        // HttpTransfers has platform-specific implementations for iOS/MacCatalyst, Android, and Windows.
        // macOS is unsupported by Shiny.Net.Http; Linux uses AddStandardHttpTransfers from the Linux head.
        // AddHttpTransfers already wires the default repository.
        s.AddHttpTransfers<SampleHttpTransferDelegate>();

        // Mirrors transfers onto a Live Activity (iOS - the widget comes from ShinyLiveActivityWidget in
        // Sample.Maui) or the foreground-service notification (Android); a no-op elsewhere.
        s.AddTransferProgress();
#endif
#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        // BLE central, BLE hosting and local notifications are wired the same way on
        // every MAUI-native platform. On Linux the head project registers these via
        // the *.Linux packages (Shiny.BluetoothLE.Linux / Shiny.Notifications.Linux /
        // Shiny.BluetoothLE.Hosting.Linux).
        s.AddBluetoothLE<SampleBleDelegate>();
        s.AddBluetoothLeHosting();
        // BLE thermal printers - Linux registers this from its head, after Shiny.BluetoothLE.Linux
        s.AddBluetoothLePrinting();
        s.AddNotifications<SampleNotificationDelegate>();

        // Source-generated GATT/L2CAP hosting - registers every [BleService] and [L2CapService]
        // class in this assembly (see BleHosting/). Emitted by Shiny.BluetoothLE.Hosting's generator.
        s.AddSingleton<Sample.Shared.Maui.BleHosting.SampleBleHostingActivity>();
        s.AddBleHostedServices();
#endif

#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        // Beacons: ranging and Eddystone work on every MAUI-native platform. Monitoring is
        // registered too - on macOS it reports NotSupported rather than throwing at startup,
        // because CoreLocation has no beacon region API on the Mac.
        s.AddBeaconRanging();
        s.AddEddystoneScanning();
        s.AddBeaconMonitoring<SampleBeaconMonitorDelegate>();
        s.AddBeaconBroadcasting();
#endif

#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        // Push: every MAUI-native platform; Linux has no Shiny.Push implementation.
        s.AddPush<SamplePushDelegate>();
#endif

#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        // Wi-Fi: capability varies wildly per platform - the sample page reads
        // IWifiManager.Capabilities and only offers what the OS actually allows.
        // Linux registers this from the Shiny.Net.Wifi.Linux package instead.
        s.AddWifi();
#endif

#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        // Screen recording: what "the screen" means differs per platform - iOS and Mac Catalyst
        // record this app's own UI only, and Windows has no audio at all. The sample page reads
        // IScreenRecorder.Capabilities and only offers what the OS actually allows.
        // Linux registers this from the Shiny.ScreenRecorder.Linux package instead.
        s.AddScreenRecorder();
#endif

#if IOS || ANDROID || MACCATALYST
        // GPS / Geofencing: Shiny.Locations only has a concrete platform
        // implementation for iOS and Android today.
        s.AddGps<SampleGpsDelegate>();
        s.AddGeofencing<SampleGeofenceDelegate>();
        s.AddMotionActivity<SampleMotionActivityDelegate>();
        // reverse geocoding: MapKit/CoreLocation on Apple, android.location.Geocoder on Android
        s.AddGeocoding();
#endif

#if IOS || ANDROID || MACCATALYST
        // Contacts: Shiny.Contacts has native implementations for iOS, Mac Catalyst, macOS and Android.
        s.AddContactStore();
        s.AddSingleton(MediaPicker.Default);
#endif

#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        // Calendar: Shiny.Calendar has native implementations on every MAUI platform
        // (EventKit on Apple, CalendarContract on Android, AppointmentStore on Windows).
        s.AddCalendarStore();
#endif

        // ── AI: GitHub Copilot chat client + the *.Extensions.AI tool surfaces ──
        // The AI Assistant page drives whichever tool bundles are registered here against a
        // Copilot IChatClient (device-code sign-in). Each bundle is gated to the platforms where
        // its underlying store is supported.
#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        s.AddSingleton<GitHubCopilotChatClientProvider>();
        s.AddCalendarAITools(b => b.AddCalendar(CalendarAICapabilities.All));
        s.AddNotificationAITools(b => b.AddReminders(ReminderAICapabilities.ReadWrite));
#endif
#if IOS || ANDROID || MACCATALYST
        s.AddContactsAITools(b => b.AddContacts(ContactAICapabilities.ReadWrite));
#endif
#if IOS || ANDROID || MACCATALYST
        s.AddLocationAITool();
#endif

        // ── Network discovery ──
        // All three protocols work on every platform this sample targets. mDNS goes through
        // NSNetService/NsdManager so it needs no entitlement; SSDP and WS-Discovery have no OS
        // API anywhere and therefore use raw multicast - see the manifest/entitlement notes in
        // Sample.Maui (Platforms/Android/AndroidManifest.xml and Sample.Maui.csproj).
        s.AddMdns();
        s.AddSsdp();
        s.AddWsDiscovery();

        // ── Printing ──
        // Thermal / receipt printers over WiFi (raw TCP 9100, found over the mDNS registered above) and
        // OS-native printing (AirPrint / PrintManager / GDI+ / CUPS) work on every platform here. CUPS backs
        // both Linux and the macOS head, which resolve Shiny.Printing's plain net10.0 build.
        s.AddNetworkPrinting();
        s.AddNativePrinting();
        s.AddPrintDocumentRendering();

#if IOS || ANDROID
        // App Functions: the orders "backend" behind the functions Siri and Gemini call. The functions themselves,
        // and AddAppFunctions(), live in the app head (Sample.Maui) - the generator only scans the app project.
        s.AddSingleton<Sample.Shared.Maui.Services.Orders.OrderStore>();
        s.AddSingleton<Sample.Shared.Maui.Services.Orders.SignInState>();
        s.AddSingleton<Sample.Shared.Maui.Services.Orders.InvocationLog>();
#endif

#if !(PLATFORM && MACOS)
        // Jobs: iOS, Android, MacCatalyst, Windows (in-proc COM-activated), and bare .NET (in-proc).
        // MacOS does not expose a background-task scheduler we wrap today.
        s.AddJob<SampleJob>(r => r.WithForeground());
#endif

        return builder;
    }
}
