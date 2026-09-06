using Xunit;

namespace Shiny.Net.Http.Tests;


public class SummaryAggregationTests
{
    [Fact]
    public void Aggregate_SumsBytesAndThroughput()
    {
        var snapshot = TransferProgressSnapshot.Aggregate(
            [
                TestData.Result("a", transferred: 30, total: 100, bytesPerSecond: 10),
                TestData.Result("b", transferred: 20, total: 100, bytesPerSecond: 5)
            ],
            HttpTransferState.InProgress
        );

        Assert.Equal(50, snapshot.Progress.BytesTransferred);
        Assert.Equal(200, snapshot.Progress.BytesToTransfer);
        Assert.Equal(15, snapshot.Progress.BytesPerSecond);
        Assert.Equal(0.25, snapshot.Fraction);
        Assert.True(snapshot.IsSummary);
    }


    [Fact]
    public void Aggregate_IsIndeterminateWhenAnyTransferHasNoKnownSize()
    {
        var snapshot = TransferProgressSnapshot.Aggregate(
            [
                TestData.Result("a", transferred: 30, total: 100),
                TestData.Result("b", transferred: 20, total: null)
            ],
            HttpTransferState.InProgress
        );

        Assert.Null(snapshot.Progress.BytesToTransfer);
        Assert.Null(snapshot.Fraction);
    }


    [Fact]
    public void Aggregate_TitlesTheBatchByCount()
    {
        var snapshot = TransferProgressSnapshot.Aggregate(
            [
                TestData.Result("a", type: TransferType.UploadRaw),
                TestData.Result("b", type: TransferType.UploadRaw),
                TestData.Result("c", type: TransferType.UploadRaw)
            ],
            HttpTransferState.InProgress
        );

        Assert.Equal("Uploading 3 files", TransferProgressContentBuilder.BuildTitle(snapshot, new()));
    }


    [Fact]
    public void Aggregate_UsesAMixedVerbForMixedDirections()
    {
        var snapshot = TransferProgressSnapshot.Aggregate(
            [TestData.Result("a", type: TransferType.UploadRaw), TestData.Result("b", type: TransferType.Download)],
            HttpTransferState.InProgress
        );

        Assert.Equal("Transferring 2 files", TransferProgressContentBuilder.BuildTitle(snapshot, new()));
        Assert.Equal("mixed", TransferProgressContentBuilder.BuildData(snapshot)["direction"]);
    }


    [Fact]
    public void RunningStatus_InProgressWinsOverPaused()
    {
        var status = TransferProgressSnapshot.RunningStatus(
            [
                TestData.Result("a", status: HttpTransferState.PausedByNoNetwork),
                TestData.Result("b", status: HttpTransferState.InProgress)
            ]
        );
        Assert.Equal(HttpTransferState.InProgress, status);
    }


    [Fact]
    public void RunningStatus_ReportsThePauseReasonWhenNothingIsMoving()
    {
        var status = TransferProgressSnapshot.RunningStatus(
            [TestData.Result("a", status: HttpTransferState.PausedByCostedNetwork)]
        );
        Assert.Equal(HttpTransferState.PausedByCostedNetwork, status);
    }


    [Fact]
    public void TerminalStatus_AFailureIsTheHeadline()
    {
        var status = TransferProgressSnapshot.TerminalStatus(
            [
                TestData.Result("a", status: HttpTransferState.Completed),
                TestData.Result("b", status: HttpTransferState.Error)
            ]
        );
        Assert.Equal(HttpTransferState.Error, status);
    }


    [Fact]
    public void TerminalStatus_CompletedBeatsCancelled()
    {
        var status = TransferProgressSnapshot.TerminalStatus(
            [
                TestData.Result("a", status: HttpTransferState.Canceled),
                TestData.Result("b", status: HttpTransferState.Completed)
            ]
        );
        Assert.Equal(HttpTransferState.Completed, status);
    }


    [Fact]
    public void Terminal_TitlesTheWholeBatch()
    {
        var snapshot = TransferProgressSnapshot.Aggregate(
            [TestData.Result("a"), TestData.Result("b"), TestData.Result("c")],
            HttpTransferState.Completed
        );

        Assert.True(snapshot.IsTerminal);
        Assert.Equal("3 transfers complete", TransferProgressContentBuilder.BuildTitle(snapshot, new()));
    }
}
