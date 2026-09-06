using Xunit;

namespace Shiny.Net.Http.Tests;


public class ProgressProjectionTests
{
    static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);


    [Fact]
    public void Projects_RangeAnchoredSoTheBarSitsAtTheCurrentFraction()
    {
        // half done at 5 bytes/sec with 50 to go => 10s remaining, 20s total, started 10s ago
        var snapshot = TestData.Snapshot(TestData.Result(transferred: 50, total: 100, bytesPerSecond: 5));
        var progress = TransferProgressContentBuilder.BuildProgress(snapshot, new(), Now);

        Assert.Equal(Now.AddSeconds(-10), progress.Start);
        Assert.Equal(Now.AddSeconds(10), progress.End);
        Assert.Null(progress.Value);
    }


    [Fact]
    public void Projects_RangeCoversTheWholeTransferSoTheBarDoesNotRestart()
    {
        // the anchored range must read back as the fraction we actually are at
        var snapshot = TestData.Snapshot(TestData.Result(transferred: 25, total: 100, bytesPerSecond: 5));
        var progress = TransferProgressContentBuilder.BuildProgress(snapshot, new(), Now);

        var total = (progress.End!.Value - progress.Start!.Value).TotalSeconds;
        var elapsed = (Now - progress.Start!.Value).TotalSeconds;

        Assert.Equal(0.25, elapsed / total, 5);
    }


    [Fact]
    public void FallsBackToFraction_WhenProjectionDisabled()
    {
        var snapshot = TestData.Snapshot(TestData.Result(transferred: 50, total: 100, bytesPerSecond: 5));
        var progress = TransferProgressContentBuilder.BuildProgress(snapshot, new() { ProjectTimeRemaining = false }, Now);

        Assert.Equal(0.5, progress.Value);
        Assert.Null(progress.Start);
    }


    [Fact]
    public void FallsBackToFraction_WhenStalled()
    {
        // no throughput means no usable estimate - a projected range would be a lie
        var snapshot = TestData.Snapshot(TestData.Result(transferred: 50, total: 100, bytesPerSecond: 0));
        var progress = TransferProgressContentBuilder.BuildProgress(snapshot, new(), Now);

        Assert.Equal(0.5, progress.Value);
        Assert.Null(progress.Start);
    }


    [Fact]
    public void FallsBackToFraction_WhenTheEstimateIsAbsurd()
    {
        // 1 byte/sec on a 1GB file projects for years
        var snapshot = TestData.Snapshot(TestData.Result(transferred: 1, total: 1_073_741_824, bytesPerSecond: 1));
        var progress = TransferProgressContentBuilder.BuildProgress(snapshot, new(), Now);

        Assert.Null(progress.Start);
        Assert.NotNull(progress.Value);
    }


    [Fact]
    public void FallsBackToFraction_WhenNotInProgress()
    {
        var snapshot = TestData.Snapshot(
            TestData.Result(transferred: 50, total: 100, bytesPerSecond: 5, status: HttpTransferState.Paused)
        );
        var progress = TransferProgressContentBuilder.BuildProgress(snapshot, new(), Now);

        Assert.Equal(0.5, progress.Value);
        Assert.Null(progress.Start);
    }


    [Fact]
    public void Indeterminate_WhenTotalSizeIsUnknown()
    {
        var snapshot = TestData.Snapshot(TestData.Result(transferred: 50, total: null));
        var progress = TransferProgressContentBuilder.BuildProgress(snapshot, new(), Now);

        Assert.True(progress.Indeterminate);
        Assert.Null(progress.Value);
        Assert.Null(progress.Start);
    }
}
