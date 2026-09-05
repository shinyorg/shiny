using Sample.tvOS.Infrastructure;
using Shiny;
using Shiny.Net;
using Shiny.Power;

namespace Sample.tvOS.Pages;


/// <summary>
/// Shiny.Core on tvOS. Worth seeing here: <see cref="IBattery"/> reports a permanently full
/// battery, because an Apple TV is mains powered and UIDevice carries no battery API on tvOS.
/// </summary>
public class StatusViewController() : ModuleViewController(
    "Shiny.Core - the same IosPlatform and IosLifecycleExecutor iOS uses"
)
{
    protected override void OnReady()
    {
        this.AddAction("Refresh", () =>
        {
            this.Refresh();
            return Task.CompletedTask;
        });

        this.AddAction("Watch network", () =>
        {
            var connectivity = Resolve<IConnectivity>();
            connectivity.Changed += (_, _) => this.Log($"connectivity -> {connectivity.Access} / {connectivity.ConnectionTypes}");
            this.Log("subscribed to IConnectivity.Changed");
            return Task.CompletedTask;
        });

        this.Refresh();
    }


    void Refresh()
    {
        var platform = Resolve<IPlatform>();
        var connectivity = Resolve<IConnectivity>();
        var battery = Resolve<IBattery>();

        this.ClearLog();
        this.Log($"IPlatform     {platform.GetType().Name}   (tvOS reuses the iOS platform layer)");
        this.Log($"Bundle        {NSBundle.MainBundle.BundleIdentifier}");
        this.Log($"tvOS          {UIDevice.CurrentDevice.SystemVersion}");
        this.Log($"AppData       {platform.AppData.FullName}");
        this.Log($"Cache         {platform.Cache.FullName}");
        this.Log($"Connectivity  {connectivity.Access} / {connectivity.ConnectionTypes}");
        this.Log($"Battery       {battery.Status} at {battery.Level:P0}   (mains powered - always Full on tvOS)");
    }
}
