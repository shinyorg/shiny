using Xunit;

namespace Shiny.Net.Http.Tests;


public class ProgressBarTests
{
    static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);


    [Fact]
    public void ToFraction_PassesAValueThrough()
        => Assert.Equal(0.4, TransferProgressBar.FromValue(0.4).ToFraction(Now));


    [Fact]
    public void ToFraction_ClampsAnOutOfRangeValue()
    {
        Assert.Equal(1d, TransferProgressBar.FromValue(1.9).ToFraction(Now));
        Assert.Equal(0d, TransferProgressBar.FromValue(-0.5).ToFraction(Now));
    }


    [Fact]
    public void ToFraction_ResolvesARangeAgainstNow()
    {
        // this is what the Android renderer does with a projected range: it cannot animate one,
        // so it collapses it to wherever the range sits right now
        var bar = TransferProgressBar.FromRange(Now.AddSeconds(-30), Now.AddSeconds(10));
        Assert.Equal(0.75, bar.ToFraction(Now));
    }


    [Fact]
    public void ToFraction_ClampsARangeOutsideItsBounds()
    {
        var bar = TransferProgressBar.FromRange(Now.AddSeconds(-60), Now.AddSeconds(-10));
        Assert.Equal(1d, bar.ToFraction(Now));
    }


    [Fact]
    public void ToFraction_IsNullWhenIndeterminate()
        => Assert.Null(TransferProgressBar.Unknown.ToFraction(Now));


    [Fact]
    public void ToFraction_IsNullForADegenerateRange()
        => Assert.Null(TransferProgressBar.FromRange(Now, Now).ToFraction(Now));
}
