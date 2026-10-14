using Shiny.Extensions.Stores;
using Shiny.Net;

namespace Shiny.DeviceTests.Tests;


public class CoreTests(
    IPlatform platform,
    Shiny.Net.IConnectivity connectivity,
    Shiny.Power.IBattery battery,
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore defaultStore,
    [FromKeyedServices(StoreKeys.Secure)] IKeyValueStore secureStore
) : DeviceTestSuite
{
    public override string Name => "Core";

    protected override void Define()
    {
        this.Test("Platform_ReportsApplicationIdentifier", _ =>
        {
            Assert.Equal("org.shiny.devicetests", platform.ApplicationIdentifier);
            Assert.NotNull(platform.AppVersion);
            return Task.CompletedTask;
        });

        this.Test("Platform_AppDataIsWritable", async ctx =>
        {
            var path = Path.Combine(platform.AppData.FullName, $"core-{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(path, "shiny", ctx.CancellationToken);
            try
            {
                Assert.Equal("shiny", await File.ReadAllTextAsync(path, ctx.CancellationToken));
            }
            finally
            {
                File.Delete(path);
            }
        });

        this.Test("Connectivity_ReportsInternet", ctx =>
        {
            ctx.Log($"Access={connectivity.Access} Types={connectivity.ConnectionTypes}");
            Assert.True(connectivity.IsInternetAvailable(), $"Expected internet access, got {connectivity.Access}");
            return Task.CompletedTask;
        });

        this.Test("Battery_ReportsLevel", ctx =>
        {
            ctx.Log($"Status={battery.Status} Level={battery.Level} Source={battery.PowerSource}");
            ctx.SkipUnless(ctx.IsAndroid || !ctx.IsSimulator, "The iOS simulator has no battery");
            Assert.InRange(battery.Level, 0.0, 1.0);
            return Task.CompletedTask;
        });

        this.Test("DefaultStore_RoundTrips", _ => RoundTrip(defaultStore));
        this.Test("SecureStore_RoundTrips", _ => RoundTrip(secureStore));
    }


    static Task RoundTrip(IKeyValueStore store)
    {
        var key = $"devicetests-{Guid.NewGuid():N}";
        store.Set(key, "value");
        try
        {
            Assert.True(store.Contains(key));
            Assert.Equal("value", store.Get<string>(key));
        }
        finally
        {
            store.Remove(key);
        }
        Assert.False(store.Contains(key));
        return Task.CompletedTask;
    }
}
