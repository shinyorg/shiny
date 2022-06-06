using Microsoft.Extensions.Configuration;
using Shiny;

namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()            
            .UseMauiApp<App>()
            // THIS IS REQUIRED TO BE DONE FOR SHINY TO RUN
            .UseShiny();

        builder.Configuration.AddJsonPlatformBundle();
        builder.Configuration.AddPlatformPreferences();

        // shiny.sensors
        builder.Services.AddAllSensors();

        // shiny.nfc
        //#if !MACCATALYST
        //        builder.Services.AddNfc();
        //#endif
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

        // shiny.push.azurenotificationhubs
        //builder.Services.AddPushAzureNotificationHubs<SamplePushDelegate>(new AzureNotificationConfig(
        //    "YourListenerConnectionString",
        //    "HubName"
        //));

        // shiny.jobs
        builder.Services.AddJob(typeof(SampleJob));
        //builder.Services.AddJobs(); // not required if using above

        // shiny.core - startup task & persistent service registration
        builder.Services.AddShinyService<StartupTask>();

        return builder.Build();
    }
}