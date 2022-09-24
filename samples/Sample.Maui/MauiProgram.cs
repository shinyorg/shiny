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
            .UseShiny() // THIS IS REQUIRED FOR SHINY ON MAUI
            .UsePrism(
                new DryIocContainerExtension(),
                prism => prism
                    .RegisterTypes(registry =>
                    {
                        // TODO: HTTP Transfers

                        // jobs
                        registry.RegisterForNavigation<Jobs.ListPage, Jobs.ListViewModel>("Jobs");
                        registry.RegisterForNavigation<Jobs.CreatePage, Jobs.CreateViewModel>("JobsCreate");

                        // beacons
                        registry.RegisterForNavigation<Beacons.CreatePage, Beacons.CreateViewModel>("BeaconCreate");
                        registry.RegisterForNavigation<Beacons.ManagedBeaconPage, Beacons.ManagedRangingViewModel>("BeaconRangingManaged");
                        registry.RegisterForNavigation<Beacons.MonitoringPage, Beacons.MonitoringViewModel>("BeaconMonitoring");
                        registry.RegisterForNavigation<Beacons.RangingPage, Beacons.RangingViewModel>("BeaconRanging");

                        // ble client
                        registry.RegisterForNavigation<BleClient.ScanPage, BleClient.ScanViewModel>("BleScan");
                        registry.RegisterForNavigation<BleClient.PeripheralPage, BleClient.PeripheralPage>("BlePeripheral");
                        registry.RegisterForNavigation<BleClient.ServicePage, BleClient.ServiceViewModel>("BlePeripheralService");
                        registry.RegisterForNavigation<BleClient.CharacteristicPage, BleClient.CharacteristicViewModel>("BlePeripheralCharacteristic");
                        registry.RegisterForNavigation<BleClient.L2CapPage, BleHosting.L2CapViewModel>("BleL2Cap");
                        registry.RegisterForNavigation<BleManaged.ManagedScanPage, BleManaged.ManagedScanViewModel>("BleManagedScan");
                        registry.RegisterForNavigation<BleManaged.ManagedPeripheralPage, BleManaged.ManagedPeripheralViewModel>("BleManagedPeripheral");

                        // ble hosting
                        registry.RegisterForNavigation<BleHosting.MainPage, BleHosting.MainViewModel>("BleHosting");
                        registry.RegisterForNavigation<BleHosting.BeaconAdvertisePage, BleHosting.BeaconAdvertiseViewModel>("BleHostingBeaconAdvertise");
                        registry.RegisterForNavigation<BleHosting.ManagedPage, BleHosting.ManagedViewModel>("BleHostingManaged");
                        registry.RegisterForNavigation<BleHosting.L2CapPage, BleHosting.L2CapViewModel>("BleHostingL2Cap");

                        // ble perf testing
                        registry.RegisterForNavigation<BlePerf.ClientPage, BlePerf.ClientViewModel>("BlePerfClient");
                        registry.RegisterForNavigation<BlePerf.ServerPage, BlePerf.ServerViewModel>("BlePerfServer");

                        // sensors
                        registry.RegisterForNavigation<Sensors.AllSensorsPage, Sensors.AllSensorsViewModel>("Sensors");
                        registry.RegisterForNavigation<Sensors.CompassPage, Sensors.CompassViewModel>("Compass");

                        // locations
                        registry.RegisterForNavigation<Geofencing.ListPage, Geofencing.ListViewModel>("Geofencing");
                        registry.RegisterForNavigation<Geofencing.CreatePage, Geofencing.CreateViewModel>("GeofencingCreate");
                        registry.RegisterForNavigation<MotionActivity.QueryPage, MotionActivity.QueryViewModel>("MotionActivityQuery");
                        registry.RegisterForNavigation<MotionActivity.FunctionsPage, MotionActivity.FunctionsViewModel>("MotionActivityFunctions");
                        registry.RegisterForNavigation<Gps.GpsPage, Gps.GpsViewModel>("GPS");

                        // notifications
                        registry.RegisterForNavigation<Notifications.PendingPage, Notifications.PendingViewModel>("NotificationsList");
                        registry.RegisterForNavigation<Notifications.OtherPage, Notifications.OtherViewModel>("NotificationsOther");
                        registry.RegisterForNavigation<Notifications.Create.CreatePage, Notifications.Create.CreateViewModel>("NotificationsCreate");
                        registry.RegisterForNavigation<Notifications.Create.IntervalPage, Notifications.Create.IntervalViewModel>("NotificationsInterval");
                        registry.RegisterForNavigation<Notifications.Create.LocationPage, Notifications.Create.LocationViewModel>("NotificationsLocation");
                        registry.RegisterForNavigation<Notifications.Create.SchedulePage, Notifications.Create.ScheduleViewModel>("NotificationsSchedule");                        
                        registry.RegisterForNavigation<Notifications.Channels.ChannelListPage, Notifications.Channels.ChannelListViewModel>("NotificationsChannelList");
                        registry.RegisterForNavigation<Notifications.Channels.ChannelCreatePage, Notifications.Channels.ChannelCreateViewModel>("NotificationsChannelCreate");

                        // speech recoginition
                        registry.RegisterForNavigation<SpeechRecognition.DictationPage, SpeechRecognition.DictationViewModel>("SrDictation");
                        registry.RegisterForNavigation<SpeechRecognition.ConversationPage, SpeechRecognition.ConversationViewModel>("SrConversation");

                        // settings/secure store
                        registry.RegisterForNavigation<Stores.BasicPage, Stores.BasicViewModel>("SettingsBasic");
                        registry.RegisterForNavigation<Stores.BindPage, Stores.BindViewModel>("SettingsBind");
                        
                        // platform
                        registry.RegisterForNavigation<Platform.ConnectivityPage, Platform.ConnectivityViewModel>("Connectivity");
                        registry.RegisterForNavigation<Platform.BatteryPage, Platform.BatteryViewModel>("Battery");

                        registry.RegisterForNavigation<MainPage, MainViewModel>();
                        registry.RegisterForNavigation<LogsPage, LogsViewModel>();
                    })
                    .OnAppStart("MainPage")
            );

        // shiny.jobs
        builder.Services.AddJobs();

        // shiny.core
        builder.Services.AddConnectivity();
        builder.Services.AddBattery();
        builder.Services.AddShinyService<Stores.AppSettings>(); // shiny's version of how to do settings

        // shiny.notifications
        builder.Services.AddNotifications<Notifications.MyNotificationDelegate>();

        // shiny.sensors
        builder.Services.AddAllSensors();

        // shiny.speechrecognition
        builder.Services.AddSpeechRecognition();

        // shiny.bluetoothle & shiny.bluetoothle.hosting
        builder.Services.AddBluetoothLE<BleClient.MyBleDelegate>();
        builder.Services.AddBleHostedCharacteristic<BleHosting.MyManagedCharacteristics>();
        builder.Services.AddBleHostedCharacteristic<BleHosting.MyManagedRequestCharacteristic>();
        builder.Services.AddBluetoothLeHosting(); // you don't need this if using AddBleHostedCharacteristic

        // shiny.beacons
        builder.Services.AddBeaconRanging();
        builder.Services.AddBeaconMonitoring<Beacons.MyBeaconMonitorDelegate>();

        // shiny.locations
        builder.Services.AddGps<Gps.MyGpsDelegate>();
        builder.Services.AddGeofencing<Geofencing.MyGeofenceDelegate>();
        //builder.Services.AddGpsDirectGeofencing<Geofencing.MyGeofenceDelegate>(); // if you don't know why this exists, don't use it!
        builder.Services.AddMotionActivity();
        
        // for platform event testing - not needed for general consumption
        builder.Services.AddShinyService<Platform.PlatformStateTests>();
        builder.Services.AddShinyService<Jobs.JobLoggerTask>();

        // services needed for samples - not shiny
        builder.Logging.AddDebug();
        //builder.Logging.AddProvider(new SqliteLoggerProvider(LogLevel.Trace));
        builder.Services.AddSingleton<SampleSqliteConnection>();
        builder.Services.AddShinyService<CommandExceptionHandler>();
        builder.Services.AddScoped<BaseServices>();
        builder.Services.AddSingleton(TextToSpeech.Default);

        return builder.Build();
    }
}
