using Shiny.CallDirectory;

namespace Shiny.DeviceTests.Tests;


public class CallDirectoryTests(ICallDirectoryManager directory) : DeviceTestSuite
{
    // 555-01xx are the North American numbers reserved for fiction; the emulator rings in national format
    const long Blocked = 18005550191;
    const long Identified = 18005550192;
    const long Allowed = 18005550193;
    const long Unlisted = 18005550194;

    // CallLog.Calls.TYPE
    const int MissedType = 3;
    const int BlockedType = 6;

    public override string Name => "CallDirectory";


    public override Task SetupAsync(TestContext context) => directory.Clear();
    public override Task TeardownAsync(TestContext context) => directory.Clear();


    protected override void Define()
    {
        this.Test("IsSupported_MatchesPlatform", ctx =>
        {
            // this app does not build the iOS extension, so only Android 10+ screens calls
            var expected = ctx.IsAndroid && OperatingSystem.IsAndroidVersionAtLeast(29);
            Assert.Equal(expected, directory.IsSupported);
            return Task.CompletedTask;
        });

        this.Test("Store_SetGetRemove", async ctx =>
        {
            await directory.Set([new(Identified, "Shiny Support"), new(Blocked, null, CallDirectoryEntryType.Block)], ctx.CancellationToken);
            Assert.Equal(2, await directory.Count(ctx.CancellationToken));
            Assert.Equal(new CallDirectoryEntry(Identified, "Shiny Support"), await directory.Get(Identified, ctx.CancellationToken));

            await directory.Set([new(Identified, "Renamed")], ctx.CancellationToken);
            Assert.Equal("Renamed", (await directory.Get(Identified, ctx.CancellationToken))?.Label);

            await directory.Remove([Blocked, Unlisted], ctx.CancellationToken);
            Assert.Null(await directory.Get(Blocked, ctx.CancellationToken));
            Assert.Equal(1, await directory.Count(ctx.CancellationToken));
        });

        this.Test("Store_ReplaceAllAndClear", async ctx =>
        {
            await directory.Set([new(Unlisted, "old")], ctx.CancellationToken);
            await directory.ReplaceAll([new(Blocked, null, CallDirectoryEntryType.Block), new(Allowed, null, CallDirectoryEntryType.Allow)], ctx.CancellationToken);

            Assert.Equal(2, await directory.Count(ctx.CancellationToken));
            Assert.Null(await directory.Get(Unlisted, ctx.CancellationToken));
            Assert.Equal(CallDirectoryEntryType.Allow, (await directory.Get(Allowed, ctx.CancellationToken))?.Type);

            await directory.Clear(ctx.CancellationToken);
            Assert.Equal(0, await directory.Count(ctx.CancellationToken));
        });

        this.Test("Reload_ReportsScreeningState", async ctx =>
        {
            var access = await directory.GetCurrentAccess();
            var result = await directory.Reload(ctx.CancellationToken);
            ctx.Log($"access {access}, reload {result}");

            if (ctx.IsAndroid)
            {
                // the runner grants the call screening role with adb, so it is held here
                Assert.Equal(AccessState.Available, access);
                Assert.Equal(CallDirectoryReloadResult.Success, result);
            }
            else
            {
                Assert.Equal(CallDirectoryReloadResult.ExtensionNotFound, result);
            }
        });

        this.Test("Screening_BlockedNumberIsBlocked", async ctx =>
        {
            ctx.RequireCapability(HostCapability.PhoneCall);
            await directory.ReplaceAll([new(Blocked, null, CallDirectoryEntryType.Block)], ctx.CancellationToken);
            await directory.Reload(ctx.CancellationToken);

            var call = await ctx.Host.PlaceCallAsync(National(Blocked), 4, ctx.CancellationToken);
            ctx.Log($"call log type {call.CallLogType}");
            Assert.Equal(BlockedType, call.CallLogType);
        }, TimeSpan.FromMinutes(1));

        this.Test("Screening_UnlistedNumberRings", async ctx =>
        {
            ctx.RequireCapability(HostCapability.PhoneCall);
            await directory.ReplaceAll([new(Blocked, null, CallDirectoryEntryType.Block)], ctx.CancellationToken);

            var call = await ctx.Host.PlaceCallAsync(National(Unlisted), 4, ctx.CancellationToken);
            ctx.Log($"call log type {call.CallLogType}");
            Assert.Equal(MissedType, call.CallLogType);
        }, TimeSpan.FromMinutes(1));

        this.Test("Screening_IdentifiedNumberRings", async ctx =>
        {
            ctx.RequireCapability(HostCapability.PhoneCall);
            await directory.ReplaceAll([new(Identified, "Shiny Support")], ctx.CancellationToken);

            var call = await ctx.Host.PlaceCallAsync(National(Identified), 4, ctx.CancellationToken);
            ctx.Log($"call log type {call.CallLogType}");
            Assert.Equal(MissedType, call.CallLogType);
        }, TimeSpan.FromMinutes(1));
    }


    // the emulator's incoming number is national - the manager adds DefaultCountryCode (1) back
    static string National(long number) => number.ToString()[1..];
}
