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
            .UseShinyShell(x => x.AddGeneratedMaps())
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var s = builder.Services;

        s.AddBluetoothLE<SampleBleDelegate>();
        s.AddBluetoothLeHosting();
        s.AddGps<SampleGpsDelegate>();
        s.AddGeofencing<SampleGeofenceDelegate>();
        s.AddNotifications<SampleNotificationDelegate>();
        s.AddPush<SamplePushDelegate>();
        s.AddHttpTransfers<SampleHttpTransferDelegate>();
        s.AddJob(typeof(SampleJob), "SampleJob");

        return builder.Build();
    }
}
