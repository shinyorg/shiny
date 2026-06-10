using Microsoft.Extensions.DependencyInjection;
using Sample.Shared.Maui.Services;

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
        s.AddMotionActivity<SampleMotionActivityDelegate>();
#endif

#if !(PLATFORM && MACOS)
        // Jobs: iOS, Android, MacCatalyst, Windows (in-proc COM-activated), and bare .NET (in-proc).
        // MacOS does not expose a background-task scheduler we wrap today.
        s.AddJob<SampleJob>(r => r.WithForeground());
#endif

        return builder;
    }
}
