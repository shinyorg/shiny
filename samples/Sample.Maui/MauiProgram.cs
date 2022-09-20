using Prism.DryIoc;
using Sample.Infrastructure;

namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny()
            .UsePrism(
                new DryIocContainerExtension(),
                prism => prism
                    .RegisterTypes(registry =>
                    {
                        registry.RegisterForNavigation<LogsPage, LogsViewModel>();

                        // platform
                        registry.RegisterForNavigation<Platform.ConnectivityPage, Platform.ConnectivityViewModel>();
                        registry.RegisterForNavigation<Platform.BatteryPage, Platform.BatteryViewModel>();

                        // beacons
                        registry.RegisterForNavigation<Beacons.CreatePage, Beacons.CreateViewModel>();
                        registry.RegisterForNavigation<Beacons.ManagedBeaconPage, Beacons.ManagedRangingViewModel>();
                        registry.RegisterForNavigation<Beacons.MonitoringPage, Beacons.MonitoringViewModel>();
                        registry.RegisterForNavigation<Beacons.RangingPage, Beacons.RangingViewModel>();

                        // ble client
                        registry.RegisterForNavigation<BleClient.ScanPage, BleClient.ScanViewModel>();
                        registry.RegisterForNavigation<BleClient.PeripheralPage, BleClient.PeripheralPage>();
                        registry.RegisterForNavigation<BleClient.ServicePage, BleClient.ServiceViewModel>();
                        registry.RegisterForNavigation<BleClient.CharacteristicPage, BleClient.CharacteristicViewModel>();
                        registry.RegisterForNavigation<BleClient.L2CapPage, BleHosting.L2CapViewModel>();
                        registry.RegisterForNavigation<BleManaged.ManagedScanPage, BleManaged.ManagedScanViewModel>();
                        registry.RegisterForNavigation<BleManaged.ManagedPeripheralPage, BleManaged.ManagedPeripheralViewModel>();

                        // ble hosting
                        registry.RegisterForNavigation<BleHosting.MainPage, BleHosting.MainViewModel>();
                        registry.RegisterForNavigation<BleHosting.BeaconAdvertisePage, BleHosting.BeaconAdvertiseViewModel>();
                        registry.RegisterForNavigation<BleHosting.ManagedPage, BleHosting.ManagedViewModel>();
                        registry.RegisterForNavigation<BleHosting.L2CapPage, BleHosting.L2CapViewModel>();

                        // sensors
                        registry.RegisterForNavigation<Sensors.AllSensorsPage, Sensors.AllSensorsViewModel>();
                        registry.RegisterForNavigation<Sensors.CompassPage, Sensors.CompassViewModel>();

                        // locations
                        registry.RegisterForNavigation<Geofencing.ListPage, Geofencing.ListViewModel>();
                        registry.RegisterForNavigation<Geofencing.CreatePage, Geofencing.CreateViewModel>();
                        registry.RegisterForNavigation<MotionActivity.ListPage, MotionActivity.ListViewModel>();
                        registry.RegisterForNavigation<MotionActivity.OtherExtensionsPage, MotionActivity.OtherExtensionsViewModel>();
                        registry.RegisterForNavigation<Gps.GpsPage, Gps.GpsViewModel>();

                        // notifications
                        registry.RegisterForNavigation<Notifications.PendingPage, Notifications.PendingViewModel>();
                        registry.RegisterForNavigation<Notifications.OtherPage, Notifications.OtherViewModel>();
                        registry.RegisterForNavigation<Notifications.Create.CreatePage, Notifications.Create.CreateViewModel>();
                        registry.RegisterForNavigation<Notifications.Create.IntervalPage, Notifications.Create.IntervalViewModel>();
                        registry.RegisterForNavigation<Notifications.Create.LocationPage, Notifications.Create.LocationViewModel>();
                        registry.RegisterForNavigation<Notifications.Create.SchedulePage, Notifications.Create.ScheduleViewModel>();
                        registry.RegisterForNavigation<Notifications.Channels.ChannelListPage, Notifications.Channels.ChannelCreateViewModel>();

                        // speech recoginition
                        registry.RegisterForNavigation<SpeechRecognition.DictationPage, SpeechRecognition.DictationViewModel>();
                        registry.RegisterForNavigation<SpeechRecognition.ConversationPage, SpeechRecognition.ConversationViewModel>();

                        // settings/secure store
                        registry.RegisterForNavigation<Stores.BasicPage, Stores.BasicViewModel>();
                        registry.RegisterForNavigation<Stores.BindPage, Stores.BindViewModel>();

                        // jobs
                        registry.RegisterForNavigation<Jobs.CreatePage, Jobs.CreateViewModel>();
                        registry.RegisterForNavigation<Jobs.ListPage, Jobs.ListViewModel>();
                    })
                    .OnAppStart("NavigationPage/LogsPage")
            );

        // services need for samples - not shiny
        builder.Logging.AddDebug();
        builder.Services.AddSingleton<SampleSqliteConnection>();
        builder.Services.AddShinyService<CommandExceptionHandler>();
        builder.Services.AddScoped<BaseServices>();
        builder.Services.AddSingleton<ITextToSpeech>(TextToSpeech.Default);

        // TODO: jobs - system, foreground, etc
        // TODO: notifications
        builder.Services.AddConnectivity();
        builder.Services.AddBattery();
        builder.Services.AddNotifications<Notifications.MyNotificationDelegate>();

        builder.Services.AddBluetoothLE<BleClient.MyBleDelegate>();
        //builder.Services.AddBleHostedCharacteristic
        builder.Services.AddBluetoothLeHosting();

        builder.Services.AddBeaconRanging();
        builder.Services.AddBeaconMonitoring<Beacons.MyBeaconMonitorDelegate>();

        builder.Services.AddGps<Gps.MyGpsDelegate>();
        builder.Services.AddGeofencing<Geofencing.MyGeofenceDelegate>();
        //builder.Services.AddGpsDirectGeofencing<Geofencing.MyGeofenceDelegate>(); // if you don't know why this exists, don't use it!
        builder.Services.AddMotionActivity();

        builder.Services.AddAllSensors();

        builder.Services.AddSpeechRecognition();

        // shiny's version of how to do settings
        builder.Services.AddShinyService<Stores.AppSettings>();

        // for platform event testing - not needed for general consumption
        builder.Services.AddShinyService<Platform.PlatformStateTests>();
        builder.Services.AddShinyService<Jobs.JobLoggerTask>();

        return builder.Build();
    }
}
