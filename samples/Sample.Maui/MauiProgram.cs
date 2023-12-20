using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Configuration;
using Sample.Dev;

namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp() => MauiApp
        .CreateBuilder()
        .UseMauiApp<App>()
        .UseShiny() // THIS IS REQUIRED FOR SHINY ON MAUI
        .UseMauiCommunityToolkitMarkup()
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        })
        .UsePrism(
            new DryIocContainerExtension(),
            prism => prism.OnAppStart("MainPage")
        )
        .RegisterLogging()
        .RegisterServices()
        .RegisterShinyServices()
        .RegisterRoutes()
        .Build();


    static MauiAppBuilder RegisterLogging(this MauiAppBuilder builder)
    {
        builder.Configuration.AddJsonPlatformBundle();
#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddDebug();
#endif
        builder.Logging.AddSqlite(Path.Combine(FileSystem.AppDataDirectory, "logging.db"), LogLevel.Debug);
#if !MACCATALYST
        builder.Logging.AddAppCenter("TEST");
#endif

        return builder;
    }

    
    static MauiAppBuilder RegisterShinyServices(this MauiAppBuilder builder)
    {
        var s = builder.Services;
        s.AddJob(typeof(SampleJob), runInForeground: true);

        // shiny.jobs
        //s.AddJobs();

        // shiny.core
        s.AddShinyService<Stores.AppSettings>(); // shiny's version of how to do settings

        // shiny.notifications
        s.AddNotifications<Notifications.MyNotificationDelegate>();

        // shiny.speechrecognition
        s.AddSpeechRecognition();

        // shiny.net.http
        s.AddHttpTransfers<HttpTransfers.MyHttpTransferDelegate>();
#if ANDROID
        s.AddShinyService<Shiny.Net.Http.PerTransferNotificationStrategy>();
#endif

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

        // for platform event testing - not needed for general consumption
        s.AddShinyService<PlatformStateTests>();
        s.AddShinyService<Jobs.JobLoggerTask>();

        return builder;
    }


    static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
    {
        var s = builder.Services;
        
        s.AddSingleton<SampleSqliteConnection>();
        s.AddShinyService<CommandExceptionHandler>();
        s.AddScoped<BaseServices>();
        s.AddSingleton(TextToSpeech.Default);
        s.AddSingleton(FilePicker.Default);
        s.AddSingleton(DeviceDisplay.Current);
        s.AddSingleton(AppInfo.Current);
        return builder;
    }


    static MauiAppBuilder RegisterRoutes(this MauiAppBuilder builder)
    {
        var s = builder.Services;

        s.RegisterForNavigation<MainPage, MainViewModel>();

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
        //s.RegisterForNavigation<BleClient.L2CapPage, BleHosting.L2CapViewModel>("BleL2Cap");
        s.RegisterForNavigation<BleManaged.ManagedScanPage, BleManaged.ManagedScanViewModel>("BleManagedScan");

        // ble hosting
        s.RegisterForNavigation<BleHosting.MainPage, BleHosting.MainViewModel>("BleHosting");
        s.RegisterForNavigation<BleHosting.BeaconAdvertisePage, BleHosting.BeaconAdvertiseViewModel>("BleHostingBeaconAdvertise");
        s.RegisterForNavigation<BleHosting.ManagedPage, BleHosting.ManagedViewModel>("BleHostingManaged");

        // locations
        s.RegisterForNavigation<Geofencing.ListPage, Geofencing.ListViewModel>("Geofencing");
        s.RegisterForNavigation<Geofencing.CreatePage, Geofencing.CreateViewModel>("GeofencingCreate");
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

        // dev
        s.RegisterForNavigation<BleHostUnitTestsPage, BleHostUnitTestsViewModel>("BleHostUnitTests");
        s.RegisterForNavigation<HttpTransfersPage, HttpTransfersViewModel>("HttpTransfersDev");
        s.RegisterForNavigation<LogsPage, LogsViewModel>();
        s.RegisterForNavigation<AppDataPage, AppDataViewModel>();
        s.RegisterForNavigation<FileViewPage, FileViewViewModel>();
        s.RegisterForNavigation<ErrorLoggingPage, ErrorLoggingViewModel>();
        s.RegisterForNavigation<SupportServicePage, SupportServiceViewModel>();
        s.RegisterForNavigation<CurrentPermissionPage, CurrentPermissionViewModel>();
        s.RegisterForNavigation<LoggerPage, LoggerViewModel>();

        return builder;
    }
}
