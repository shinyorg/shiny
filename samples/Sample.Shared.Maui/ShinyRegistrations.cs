using Microsoft.Maui.Hosting;

namespace Sample.Shared.Maui;

public static class ShinyRegistrations
{
    public static MauiAppBuilder UseSampleShiny(this MauiAppBuilder builder)
    {
        builder
            .UseShinyTableView()
            .UseShinyShell(x => x.AddGeneratedMaps())
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var s = builder.Services;

#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        s.AddBattery();
        s.AddConnectivity();
#endif

#if IOS || ANDROID || MACCATALYST || WINDOWS
        // HttpTransfers has platform-specific implementations for iOS/MacCatalyst, Android, and Windows.
        // macOS is unsupported by Shiny.Net.Http; Linux uses AddStandardHttpTransfers from the Linux head.
        // AddHttpTransfers already wires the default repository.
        s.AddHttpTransfers<SampleHttpTransferDelegate>();
#endif
#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        // BLE central, BLE hosting and local notifications are wired the same way on
        // every MAUI-native platform. On Linux the head project registers these via
        // the *.Linux packages (Shiny.BluetoothLE.Linux / Shiny.Notifications.Linux /
        // Shiny.BluetoothLE.Hosting.Linux).
        s.AddBluetoothLE<SampleBleDelegate>();
        s.AddBluetoothLeHosting();
        s.AddNotifications<SampleNotificationDelegate>();
#endif

#if IOS || ANDROID || MACCATALYST || MACOS || WINDOWS
        // Push: every MAUI-native platform; Linux has no Shiny.Push implementation.
        s.AddPush<SamplePushDelegate>();
#endif

#if IOS || ANDROID || MACCATALYST
        // GPS / Geofencing: Shiny.Locations only has a concrete platform
        // implementation for iOS and Android today.
        s.AddGps<SampleGpsDelegate>();
        s.AddGeofencing<SampleGeofenceDelegate>();
#endif

#if !MACOS && !WINDOWS
        // Jobs: iOS, Android, MacCatalyst, and plain net (Linux head).
        // MacOS and Windows samples do not exercise the jobs subsystem.
        s.AddJob(typeof(SampleJob), "SampleJob");
#endif

        return builder;
    }
}
