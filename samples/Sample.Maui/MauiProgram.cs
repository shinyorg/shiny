using Sample.Maui.Delegates;

namespace Sample.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny()
            .UseShinyTableView()
            .UseShinyShell(x => x.AddGeneratedMaps())
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var s = builder.Services;

        s.AddBluetoothLE<SampleBleDelegate>();
        s.AddConnectivity();
        s.AddBattery();

#if !WINDOWS
        s.AddBluetoothLeHosting();
        s.AddGps<SampleGpsDelegate>();
        s.AddGeofencing<SampleGeofenceDelegate>();
        s.AddNotifications<SampleNotificationDelegate>();
        s.AddPush<SamplePushDelegate>();
        s.AddHttpTransfers<SampleHttpTransferDelegate>();
        s.AddJobs();
        s.AddJob(typeof(SampleJob), "SampleJob");
#endif

        return builder.Build();
    }
}
