using Shiny.AgeSignals;

namespace Shiny.DeviceTests.Tests;


public class AgeSignalsTests(IAgeSignals ageSignals) : DeviceTestSuite
{
    public override string Name => "AgeSignals";


    protected override void Define()
    {
        this.Test("Eligibility_MatchesPlatform", async ctx =>
        {
            // Mac Catalyst has no implementation; iOS answers from 26, Android always has the Play API
            var expected = ctx.IsAndroid || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst() && OperatingSystem.IsIOSVersionAtLeast(26));

            AgeSignalsEligibility result;
            try
            {
                result = await ageSignals.GetEligibility(ctx.CancellationToken);
            }
            catch (AgeSignalsException ex) when (ex.Reason is AgeSignalsErrorReason.NotAvailable or AgeSignalsErrorReason.InvalidAccount)
            {
                // the native call ran and answered - that is what this test proves - but there is no account to ask about
                TestContext.Skip($"Declared Age Range has no account to answer for ({ex.Reason}): {ex.Message}");
                return;
            }
            ctx.Log($"eligibility {result}");
            Assert.Equal(expected, result.IsSupported);

            if (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst() && OperatingSystem.IsIOSVersionAtLeast(26, 2))
                Assert.NotNull(result.IsEligible);

            if (ctx.IsAndroid || !expected)
                Assert.Null(result.IsEligible);
        });

        this.Test("Request_RejectsBadGates", async ctx =>
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ageSignals.RequestAgeRange(18, 13, cancellationToken: ctx.CancellationToken));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ageSignals.RequestAgeRange(0, cancellationToken: ctx.CancellationToken));
        });

        this.Test("Request_NotSupported_OffPlatform", async ctx =>
        {
            ctx.SkipUnless(OperatingSystem.IsMacCatalyst(), "Only Mac Catalyst has no implementation in this app");
            Assert.Equal(AgeSignalStatus.NotSupported, (await ageSignals.RequestAgeRange(13, 16, 18, ctx.CancellationToken)).Status);
            Assert.False(await ageSignals.ShowSignificantUpdate("test", ctx.CancellationToken));
        });

        this.Test("Request_ReachesPlay", async ctx =>
        {
            // Play's US-law flow never prompts in-app, so this completes headless. Without a Play account (or when the
            // app wasn't installed from Play) Play answers with an error code - mapped, not Unknown, proves the binding.
            ctx.RequirePlatform("android");
            try
            {
                var signal = await ageSignals.RequestAgeRange(13, 16, 18, ctx.CancellationToken);
                ctx.Log($"signal {signal}");
                Assert.NotEqual(AgeSignalStatus.NotSupported, signal.Status);
            }
            catch (AgeSignalsException ex)
            {
                ctx.Log($"Play answered {ex.Reason} ({ex.NativeCode}): {ex.Message}");
                Assert.NotNull(ex.NativeCode);
            }
        });
    }
}
