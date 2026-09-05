using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.tvOS.Pages;
using Sample.tvOS.Services;
using Shiny;
using Shiny.Data.Sync;
using Shiny.Hosting;
using Shiny.Jobs;

namespace Sample.tvOS;


/// <summary>
/// The whole tvOS hosting story: inherit <see cref="ShinyAppDelegate"/> — the same one iOS uses —
/// and hand back a host. There is no MAUI on tvOS, so this is the only route.
/// </summary>
[Register(nameof(AppDelegate))]
public class AppDelegate : ShinyAppDelegate
{
    public override UIWindow? Window { get; set; }


    protected override IHost CreateShinyHost()
    {
        var builder = HostBuilder.Create();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        // central role only - tvOS has no peripheral role, so there is no Shiny.BluetoothLE.Hosting here
        builder.Services.AddBluetoothLE();

        // Bonjour through NSNetService, exactly as on iOS
        builder.Services.AddMdns();

        // BGTaskScheduler - see BGTaskSchedulerPermittedIdentifiers in Info.plist
        builder.Services.AddJob<HeartbeatJob>(reg => reg.WithInternet(InternetAccess.Any));

        // background NSUrlSession
        builder.Services.AddHttpTransfers<SampleTransferDelegate>();

        // APNs. tvOS gets silent push and the badge - never a tap-through
        builder.Services.AddPush<SamplePushDelegate>();

        // ReplayKit. tvOS advertises everything iOS does except Microphone
        builder.Services.AddScreenRecorder();

        // NSUrlSession-backed outbox/inbox
        builder.Services.AddDataSync<SampleSyncDelegate>(b => b.RegisterEndpoint<Viewing>(
            "https://httpbin.org/anything/viewings",
            e => e.Direction = SyncDirection.PushOnly
        ));

        builder.Services.AddSingleton<AppLog>();

        return builder.Build();
    }


    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // base builds and runs the Shiny host - do that before resolving anything out of it
        base.FinishedLaunching(application, launchOptions);

        var tabs = new UITabBarController
        {
            ViewControllers =
            [
                Wrap(new StatusViewController(), "Status"),
                Wrap(new BleViewController(), "BLE"),
                Wrap(new DiscoveryViewController(), "mDNS"),
                Wrap(new JobsViewController(), "Jobs"),
                Wrap(new TransfersViewController(), "HTTP"),
                Wrap(new PushViewController(), "Push"),
                Wrap(new RecorderViewController(), "Record"),
                Wrap(new DataSyncViewController(), "Sync")
            ]
        };

        this.Window = new UIWindow(UIScreen.MainScreen.Bounds)
        {
            RootViewController = tabs
        };
        this.Window.MakeKeyAndVisible();

        return true;
    }


    static UIViewController Wrap(UIViewController controller, string title)
    {
        controller.Title = title;
        controller.TabBarItem = new UITabBarItem(title, null, 0);
        return controller;
    }
}
