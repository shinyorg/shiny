using Shiny;

namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .UseShiny()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddAllSensors();
        builder.Services.AddNfc();
        builder.Services.AddBeaconRanging();
        builder.Services.AddBeaconMonitoring<SampleBeaconMonitorDelegate>();
        //builder.Services.AddGpsDirectGeofencing<SampleGpsDelegate>();
        //builder.Services.AddGps<SampleGpsDelegate>();
        //builder.Services.AddGeofencing<SampleGeofenceDelegate>();
        //builder.Services.AddMotionActivity();
        builder.Services.AddNotifications<SampleNotificationDelegate>();
        builder.Services.AddBluetoothLE<SampleBleDelegate>();
        builder.Services.AddHttpTransfers<SampleHttpTransferDelegate>();
        builder.Services.AddBluetoothLeHosting();
        builder.Services.AddSpeechRecognition();

        //builder.Services.AddJob(typeof(SampleJob));
        builder.Services.AddShinyService<StartupTask>();


        var app = builder.Build();
        app.RunShiny();

        return app;
    }
}