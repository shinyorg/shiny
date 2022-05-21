using Shiny;

namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()

            // THIS IS REQUIRED TO BE DONE FOR SHINY TO RUN
            .UseMauiApp<App>()
            .UseShiny()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // shiny.sensors
        builder.Services.AddAllSensors();

        // shiny.nfc
        builder.Services.AddNfc();

        // shiny.locations
        builder.Services.AddGps<SampleGpsDelegate>();
        builder.Services.AddGeofencing<SampleGeofenceDelegate>();
        builder.Services.AddMotionActivity();

        // shiny.notifications
        builder.Services.AddNotifications<SampleNotificationDelegate>();

        // shiny.bluetoothle
        builder.Services.AddBluetoothLE<SampleBleDelegate>();

        // shiny.bluetoothle.hosting
        builder.Services.AddBluetoothLeHosting();

        // shiny.beacons
        builder.Services.AddBeaconRanging();
        builder.Services.AddBeaconMonitoring<SampleBeaconMonitorDelegate>();

        // shiny.net.http
        builder.Services.AddHttpTransfers<SampleHttpTransferDelegate>();

        // shiny.speechrecognition
        builder.Services.AddSpeechRecognition();

        // shiny.push
        builder.Services.AddPush<SamplePushDelegate>();

        // shiny.jobs
        builder.Services.AddJob(typeof(SampleJob));
        builder.Services.AddJobs(); // not required if using above

        // shiny.core - startup task & persistent service registration
        builder.Services.AddShinyService<StartupTask>();

        // THIS IS REQUIRED TO BE DONE FOR SHINY TO RUN
        var app = builder.Build();
        app.RunShiny();

        return app;
    }
}