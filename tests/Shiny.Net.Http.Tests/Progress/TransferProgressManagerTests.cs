using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Net.Http.Tests.Fakes;
using Xunit;

namespace Shiny.Net.Http.Tests;


public class TransferProgressManagerTests
{
    static (TransferProgressManager Manager, FakeTransferManager Transfers, FakeProgressRenderer Renderer) Build(
        Action<TransferProgressOptions>? configure = null,
        bool rendererAvailable = true
    )
    {
        var options = new TransferProgressOptions
        {
            // take coalescing out of the picture unless a test asks for it
            MinimumUpdateInterval = TimeSpan.Zero,
            MinimumPercentChange = 0d
        };
        configure?.Invoke(options);

        var transfers = new FakeTransferManager();
        var renderer = new FakeProgressRenderer(rendererAvailable);
        var manager = new TransferProgressManager(
            transfers,
            [renderer],
            options,
            NullLogger<TransferProgressManager>.Instance
        );
        return (manager, transfers, renderer);
    }


    [Fact]
    public void UnavailableRenderer_IsNeverSubscribedTo()
    {
        var (manager, transfers, renderer) = Build(rendererAvailable: false);
        manager.Start();

        Assert.False(transfers.HasSubscribers);
        transfers.Raise(TestData.Result());
        Assert.Empty(renderer.Shown);
    }


    [Fact]
    public void Summary_AggregatesAcrossTransfers()
    {
        var (manager, transfers, renderer) = Build();
        manager.Start();

        transfers.Raise(TestData.Result("a", transferred: 30, total: 100));
        transfers.Raise(TestData.Result("b", transferred: 20, total: 100));

        var last = renderer.Shown[^1];
        Assert.Equal(TransferProgressManager.SummaryKey, last.Key);
        Assert.Equal("25%", last.Content.ShortStatus);
        Assert.Equal("Downloading 2 files", last.Content.Title);
    }


    [Fact]
    public void Summary_ProgressNeverWalksBackwardsWhenOneOfABatchCompletes()
    {
        var (manager, transfers, renderer) = Build(o => o.ProjectTimeRemaining = false);
        manager.Start();

        transfers.Raise(TestData.Result("a", transferred: 100, total: 100));
        transfers.Raise(TestData.Result("b", transferred: 0, total: 100));
        var before = renderer.Shown[^1].Content.Progress!.ToFraction();

        // 'a' finishes - it must stay in the aggregate, or the bar would jump from 50% back to 0%
        transfers.Raise(TestData.Result("a", transferred: 100, total: 100, status: HttpTransferState.Completed));
        var after = renderer.Shown[^1].Content.Progress!.ToFraction();

        Assert.Equal(0.5, before);
        Assert.Equal(0.5, after);
    }


    [Fact]
    public void Summary_HidesOnlyWhenTheWholeBatchIsDone()
    {
        var (manager, transfers, renderer) = Build();
        manager.Start();

        transfers.Raise(TestData.Result("a", transferred: 50, total: 100));
        transfers.Raise(TestData.Result("b", transferred: 50, total: 100));
        transfers.Raise(TestData.Result("a", status: HttpTransferState.Completed, transferred: 100, total: 100));

        Assert.Empty(renderer.Hidden);

        transfers.Raise(TestData.Result("b", status: HttpTransferState.Completed, transferred: 100, total: 100));

        var hidden = Assert.Single(renderer.Hidden);
        Assert.Equal(TransferProgressManager.SummaryKey, hidden.Key);
        Assert.Equal("2 transfers complete", hidden.Content.Title);
    }


    [Fact]
    public void PerTransfer_KeysEachSurfaceByTransferId()
    {
        var (manager, transfers, renderer) = Build(o => o.Scope = TransferProgressScope.PerTransfer);
        manager.Start();

        transfers.Raise(TestData.Result("a", transferred: 10, total: 100));
        transfers.Raise(TestData.Result("b", transferred: 20, total: 100));

        Assert.Equal(["a", "b"], renderer.Shown.Select(x => x.Key));
    }


    [Fact]
    public void PerTransfer_HidesThatTransferOnly()
    {
        var (manager, transfers, renderer) = Build(o => o.Scope = TransferProgressScope.PerTransfer);
        manager.Start();

        transfers.Raise(TestData.Result("a", transferred: 10, total: 100));
        transfers.Raise(TestData.Result("a", status: HttpTransferState.Error, exception: new InvalidOperationException("boom")));

        var hidden = Assert.Single(renderer.Hidden);
        Assert.Equal("a", hidden.Key);
        Assert.Equal("Download failed", hidden.Content.Title);
        Assert.Equal("boom", hidden.Content.Body);
    }


    [Fact]
    public void Coalescing_DropsUpdatesThatAreTooCloseTogether()
    {
        var (manager, transfers, renderer) = Build(o =>
        {
            o.MinimumUpdateInterval = TimeSpan.FromMinutes(5);
            o.MinimumPercentChange = 0.5;
            o.Scope = TransferProgressScope.PerTransfer;
        });
        manager.Start();

        transfers.Raise(TestData.Result("a", transferred: 10, total: 100));
        transfers.Raise(TestData.Result("a", transferred: 11, total: 100));
        transfers.Raise(TestData.Result("a", transferred: 12, total: 100));

        // the first always draws; the rest are inside the interval and under the percent threshold
        Assert.Single(renderer.Shown);
    }


    [Fact]
    public void Coalescing_NeverSwallowsAStateChange()
    {
        var (manager, transfers, renderer) = Build(o =>
        {
            o.MinimumUpdateInterval = TimeSpan.FromMinutes(5);
            o.MinimumPercentChange = 0.5;
            o.Scope = TransferProgressScope.PerTransfer;
        });
        manager.Start();

        transfers.Raise(TestData.Result("a", transferred: 10, total: 100));
        transfers.Raise(TestData.Result("a", transferred: 10, total: 100, status: HttpTransferState.PausedByNoNetwork));

        Assert.Equal(2, renderer.Shown.Count);
        Assert.Equal("Waiting for a connection", renderer.Shown[^1].Content.Title);
    }


    [Fact]
    public async Task Start_ReconcilesAgainstTheTransfersStillQueued()
    {
        var (manager, transfers, renderer) = Build(o => o.Scope = TransferProgressScope.PerTransfer);
        transfers.Add(
            new HttpTransfer(new HttpTransferRequest("a", "https://x/y", TransferType.Download, "/tmp/y"), 100, 0, HttpTransferState.Pending, DateTimeOffset.UtcNow)
        );

        manager.Start();
        await Task.Delay(50);   // Start is async void - let the reconcile land

        Assert.Equal(["a"], renderer.ReconciledWith!);
    }


    [Fact]
    public void Dispose_Unsubscribes()
    {
        var (manager, transfers, renderer) = Build();
        manager.Start();
        manager.Dispose();

        transfers.Raise(TestData.Result());
        Assert.Empty(renderer.Shown);
    }
}
