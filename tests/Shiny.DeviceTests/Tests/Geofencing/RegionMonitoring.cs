#if IOS || MACCATALYST
using CoreFoundation;
using CoreLocation;
#endif

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Whether the OS can monitor regions here at all. Some iOS simulators cannot: CLMonitor reports every
/// condition as Unmonitored straight away (and CLLocationManager region monitoring fails with
/// kCLErrorRegionMonitoringFailure), so no state or transition ever arrives.
/// </summary>
static class RegionMonitoring
{
    static string? unavailableReason;
    static bool checkedOnce;


    public static async Task RequireAsync(TestContext ctx)
    {
        if (!checkedOnce)
        {
            unavailableReason = await CheckAsync(ctx);
            checkedOnce = true;
        }
        ctx.SkipUnless(unavailableReason == null, unavailableReason ?? String.Empty);
    }


#if IOS || MACCATALYST
    static async Task<string?> CheckAsync(TestContext ctx)
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(18) && !OperatingSystem.IsMacCatalystVersionAtLeast(18))
            return null;

        var name = $"devicetestsprobe{Guid.NewGuid():N}";
        var mon = await CLMonitor.RequestMonitorAsync(CLMonitorConfiguration.Create(name, DispatchQueue.MainQueue, (_, _) => { }));
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() => mon.AddCondition(
                new CLCircularGeographicCondition(new CLLocationCoordinate2D(SimulatedLocation.B.Latitude, SimulatedLocation.B.Longitude), 200),
                "probe"
            ));

            // a monitored condition starts Unknown and settles on Satisfied/Unsatisfied; an unmonitorable one is Unmonitored at once
            var state = CLMonitoringState.Unknown;
            for (var i = 0; i < 10 && state == CLMonitoringState.Unknown; i++)
            {
                await Task.Delay(500, ctx.CancellationToken);
                state = await MainThread.InvokeOnMainThreadAsync(() => mon.GetMonitoringRecord("probe")?.LastEvent.State ?? CLMonitoringState.Unknown);
            }
            ctx.Log($"Region monitoring probe: {state}");

            return state == CLMonitoringState.Unmonitored
                ? "CoreLocation cannot monitor regions here - CLMonitor reports the condition as Unmonitored (some iOS simulators)"
                : null;
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() => mon.RemoveCondition("probe"));
            mon.Dispose();
        }
    }
#else
    static Task<string?> CheckAsync(TestContext ctx) => Task.FromResult<string?>(null);
#endif
}
