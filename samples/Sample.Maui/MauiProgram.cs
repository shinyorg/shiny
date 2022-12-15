using Prism.DryIoc;

namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp() => MauiApp
        .CreateBuilder()
        .UseMauiApp<App>()
        .UseShiny() // THIS IS REQUIRED FOR SHINY ON MAUI
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        })
        .UsePrism(
            new DryIocContainerExtension(),
            prism => prism.OnAppStart("MainPage")
        )
        .RegisterServices()
        .RegisterShinyServices()
        .RegisterRoutes()
        .Build();


    static MauiAppBuilder RegisterShinyServices(this MauiAppBuilder builder)
    {
        var s = builder.Services;
#if DEBUG
        builder.Logging.AddDebug();
#endif
        //builder.Logging.AddAppCenter("")

        s.AddJob(typeof(SampleJob));

        // shiny.jobs
        //s.AddJobs();

        // shiny.core
        s.AddConnectivity();
        s.AddBattery();
        s.AddShinyService<Stores.AppSettings>(); // shiny's version of how to do settings

        // shiny.notifications
        s.AddNotifications<Notifications.MyNotificationDelegate>();

        // shiny.speechrecognition
        s.AddSpeechRecognition();

        // shiny.net.http
        s.AddHttpTransfers<HttpTransfers.MyHttpTransferDelegate>();

        // shiny.bluetoothle & shiny.bluetoothle.hosting
        s.AddBluetoothLE<BleClient.MyBleDelegate>();
        s.AddBleHostedCharacteristic<BleHosting.MyManagedCharacteristics>();
        s.AddBleHostedCharacteristic<BleHosting.MyManagedRequestCharacteristic>();
        s.AddBluetoothLeHosting(); // you don't need this if using AddBleHostedCharacteristic

        // shiny.beacons
        s.AddBeaconRanging();
        s.AddBeaconMonitoring<Beacons.MyBeaconMonitorDelegate>();

        // shiny.locations
        s.AddGps<Gps.MyGpsDelegate>();
        s.AddGeofencing<Geofencing.MyGeofenceDelegate>();
        //s.AddGpsDirectGeofencing<Geofencing.MyGeofenceDelegate>(); // if you don't know why this exists, don't use it!
        s.AddMotionActivity();

        // for platform event testing - not needed for general consumption
        s.AddShinyService<Platform.PlatformStateTests>();
        s.AddShinyService<Jobs.JobLoggerTask>();

        return builder;
    }


    static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
    {
        var s = builder.Services;

        //builder.Logging.AddProvider(new SqliteLoggerProvider(LogLevel.Trace));
        s.AddSingleton<SampleSqliteConnection>();
        s.AddShinyService<CommandExceptionHandler>();
        s.AddScoped<BaseServices>();
        s.AddSingleton(TextToSpeech.Default);
        s.AddSingleton(FilePicker.Default);

        return builder;
    }


    static MauiAppBuilder RegisterRoutes(this MauiAppBuilder builder)
    {
        var s = builder.Services;

        // HTTP Transfers
        s.RegisterForNavigation<HttpTransfers.CreatePage, HttpTransfers.CreateViewModel>("HttpTransfersCreate");
        s.RegisterForNavigation<HttpTransfers.PendingPage, HttpTransfers.PendingViewModel>("HttpTransfers");

        // jobs
        s.RegisterForNavigation<Jobs.ListPage, Jobs.ListViewModel>("Jobs");
        s.RegisterForNavigation<Jobs.CreatePage, Jobs.CreateViewModel>("JobsCreate");

        // beacons
        s.RegisterForNavigation<Beacons.CreatePage, Beacons.CreateViewModel>("BeaconCreate");
        s.RegisterForNavigation<Beacons.ManagedBeaconPage, Beacons.ManagedRangingViewModel>("BeaconRangingManaged");
        s.RegisterForNavigation<Beacons.MonitoringPage, Beacons.MonitoringViewModel>("BeaconMonitoring");
        s.RegisterForNavigation<Beacons.RangingPage, Beacons.RangingViewModel>("BeaconRanging");

        // ble client
        s.RegisterForNavigation<BleClient.ScanPage, BleClient.ScanViewModel>("BleScan");
        s.RegisterForNavigation<BleClient.PeripheralPage, BleClient.PeripheralViewModel>("BlePeripheral");
        s.RegisterForNavigation<BleClient.ServicePage, BleClient.ServiceViewModel>("BlePeripheralService");
        s.RegisterForNavigation<BleClient.CharacteristicPage, BleClient.CharacteristicViewModel>("BlePeripheralCharacteristic");
        s.RegisterForNavigation<BleClient.L2CapPage, BleHosting.L2CapViewModel>("BleL2Cap");
        s.RegisterForNavigation<BleManaged.ManagedScanPage, BleManaged.ManagedScanViewModel>("BleManagedScan");
        s.RegisterForNavigation<BleManaged.ManagedPeripheralPage, BleManaged.ManagedPeripheralViewModel>("BleManagedPeripheral");

        // ble hosting
        s.RegisterForNavigation<BleHosting.MainPage, BleHosting.MainViewModel>("BleHosting");
        s.RegisterForNavigation<BleHosting.BeaconAdvertisePage, BleHosting.BeaconAdvertiseViewModel>("BleHostingBeaconAdvertise");
        s.RegisterForNavigation<BleHosting.ManagedPage, BleHosting.ManagedViewModel>("BleHostingManaged");
        s.RegisterForNavigation<BleHosting.L2CapPage, BleHosting.L2CapViewModel>("BleHostingL2Cap");

        // ble perf testing
        s.RegisterForNavigation<BlePerf.ClientPage, BlePerf.ClientViewModel>("BlePerfClient");
        s.RegisterForNavigation<BlePerf.ServerPage, BlePerf.ServerViewModel>("BlePerfServer");

        // locations
        s.RegisterForNavigation<Geofencing.ListPage, Geofencing.ListViewModel>("Geofencing");
        s.RegisterForNavigation<Geofencing.CreatePage, Geofencing.CreateViewModel>("GeofencingCreate");
        s.RegisterForNavigation<MotionActivity.QueryPage, MotionActivity.QueryViewModel>("MotionActivityQuery");
        s.RegisterForNavigation<Gps.GpsPage, Gps.GpsViewModel>("GPS");

        // notifications
        s.RegisterForNavigation<Notifications.PendingPage, Notifications.PendingViewModel>("NotificationsList");
        s.RegisterForNavigation<Notifications.OtherPage, Notifications.OtherViewModel>("NotificationsOther");
        s.RegisterForNavigation<Notifications.Create.CreatePage, Notifications.Create.CreateViewModel>("NotificationsCreate");
        s.RegisterForNavigation<Notifications.Create.IntervalPage, Notifications.Create.IntervalViewModel>("NotificationsInterval");
        s.RegisterForNavigation<Notifications.Create.LocationPage, Notifications.Create.LocationViewModel>("NotificationsLocation");
        s.RegisterForNavigation<Notifications.Create.SchedulePage, Notifications.Create.ScheduleViewModel>("NotificationsSchedule");
        s.RegisterForNavigation<Notifications.Channels.ChannelListPage, Notifications.Channels.ChannelListViewModel>("NotificationsChannelList");
        s.RegisterForNavigation<Notifications.Channels.ChannelCreatePage, Notifications.Channels.ChannelCreateViewModel>("NotificationsChannelCreate");

        // speech recoginition
        s.RegisterForNavigation<SpeechRecognition.DictationPage, SpeechRecognition.DictationViewModel>("SrDictation");
        s.RegisterForNavigation<SpeechRecognition.ConversationPage, SpeechRecognition.ConversationViewModel>("SrConversation");

        // settings/secure store
        s.RegisterForNavigation<Stores.BasicPage, Stores.BasicViewModel>("SettingsBasic");
        s.RegisterForNavigation<Stores.BindPage, Stores.BindViewModel>("SettingsBind");

        // platform
        s.RegisterForNavigation<Platform.ConnectivityPage, Platform.ConnectivityViewModel>("Connectivity");
        s.RegisterForNavigation<Platform.BatteryPage, Platform.BatteryViewModel>("Battery");

        s.RegisterForNavigation<MainPage, MainViewModel>();
        s.RegisterForNavigation<LogsPage, LogsViewModel>();

        return builder;
    }
}
