using System.Diagnostics;
#if IOS || MACCATALYST
using CoreLocation;
#endif

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// What a request for always-on location should answer, read straight from CoreLocation. The runner normally
/// pre-grants always; run with --no-grant after <c>simctl privacy grant location</c> (when-in-use) or
/// <c>simctl privacy revoke location</c> to cover the other answers.
/// </summary>
public static class LocationAuthorization
{
    public static AccessState ExpectedForAlways()
    {
#if IOS || MACCATALYST
        if (!CLLocationManager.LocationServicesEnabled)
            return AccessState.Disabled;

        using var manager = new CLLocationManager();
        var status = manager.AuthorizationStatus;
        if (status == CLAuthorizationStatus.NotDetermined)
            TestContext.Skip("Location is not determined - answering the prompt needs a person");

        return status switch
        {
            CLAuthorizationStatus.AuthorizedAlways => AccessState.Available,
            CLAuthorizationStatus.Denied => AccessState.Denied,
            _ => AccessState.Restricted // when-in-use, or restricted by the device
        };
#else
        TestContext.Skip("CoreLocation authorization only");
        return AccessState.Unknown;
#endif
    }


    /// <summary>
    /// Asks twice. The first ask may sit on iOS's one-time always upgrade prompt (nobody answers it on a
    /// headless simulator, so it runs to the 30s cap); the second must not wait for anything.
    /// </summary>
    public static async Task AssertAnswersForAlways(TestContext ctx, string name, Func<Task<AccessState>> request)
    {
        var expected = ExpectedForAlways();
        var watch = Stopwatch.StartNew();
        var first = await request();
        ctx.Log($"{name}: {first} in {watch.ElapsedMilliseconds}ms (system says {expected})");

        watch.Restart();
        var second = await request();
        ctx.Log($"{name} again: {second} in {watch.ElapsedMilliseconds}ms");

        Assert.Equal(expected, first);
        Assert.Equal(expected, second);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"the second {name} took {watch.Elapsed}");
    }
}
