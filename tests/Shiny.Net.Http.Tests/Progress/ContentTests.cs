using Xunit;

namespace Shiny.Net.Http.Tests;


public class ContentTests
{
    [Fact]
    public void Title_NamesTheFileAndDirection()
    {
        var snapshot = TestData.Snapshot(TestData.Result(type: TransferType.UploadMultipart, localPath: "/tmp/receipt.pdf"));
        Assert.Equal("Uploading receipt.pdf", TransferProgressContentBuilder.BuildTitle(snapshot, new()));
    }


    [Fact]
    public void Title_DropsTheFileNameWhenTheFieldIsOff()
    {
        var snapshot = TestData.Snapshot(TestData.Result(type: TransferType.UploadMultipart, localPath: "/tmp/receipt.pdf"));
        var options = new TransferProgressOptions { Fields = TransferProgressFields.Direction };

        Assert.Equal("Uploading", TransferProgressContentBuilder.BuildTitle(snapshot, options));
    }


    [Theory]
    [InlineData(HttpTransferState.Completed, "Download complete")]
    [InlineData(HttpTransferState.Error, "Download failed")]
    [InlineData(HttpTransferState.Canceled, "Download cancelled")]
    [InlineData(HttpTransferState.Paused, "Downloading paused")]
    [InlineData(HttpTransferState.PausedByNoNetwork, "Waiting for a connection")]
    [InlineData(HttpTransferState.PausedByCostedNetwork, "Waiting for Wi-Fi")]
    public void Title_ReflectsTerminalAndPausedStates(HttpTransferState state, string expected)
    {
        var snapshot = TestData.Snapshot(TestData.Result(status: state));
        Assert.Equal(expected, TransferProgressContentBuilder.BuildTitle(snapshot, new()));
    }


    [Fact]
    public void Body_CombinesTheSelectedFields()
    {
        var snapshot = TestData.Snapshot(TestData.Result(
            transferred: 13_002_342,
            total: 50_331_648,
            bytesPerSecond: 1_572_864
        ));

        // percent is the short status here, so it is deliberately not repeated in the body
        Assert.Equal("12 MB of 48 MB · 1.5 MB/s · 23s left", TransferProgressContentBuilder.BuildBody(snapshot, new()));
    }


    [Fact]
    public void Body_IncludesPercentWhenTheChipShowsSomethingElse()
    {
        var snapshot = TestData.Snapshot(TestData.Result(transferred: 41, total: 100));
        var options = new TransferProgressOptions
        {
            Fields = TransferProgressFields.Percent,
            ShortStatus = TransferProgressShortStatus.Speed
        };

        Assert.Equal("41%", TransferProgressContentBuilder.BuildBody(snapshot, options));
    }


    [Fact]
    public void Body_IsNullWhenNoFieldHasAnythingToSay()
    {
        var snapshot = TestData.Snapshot();
        var options = new TransferProgressOptions { Fields = TransferProgressFields.None };

        Assert.Null(TransferProgressContentBuilder.BuildBody(snapshot, options));
    }


    [Fact]
    public void Body_CarriesTheErrorMessage()
    {
        var snapshot = TestData.Snapshot(TestData.Result(
            status: HttpTransferState.Error,
            exception: new InvalidOperationException("Invalid Status Code: 503")
        ));

        Assert.Equal("Invalid Status Code: 503", TransferProgressContentBuilder.BuildBody(snapshot, new()));
    }


    [Theory]
    [InlineData(TransferProgressShortStatus.Percent, "41%")]
    [InlineData(TransferProgressShortStatus.TimeRemaining, "57s")]
    [InlineData(TransferProgressShortStatus.Speed, "1.0 KB/s")]
    [InlineData(TransferProgressShortStatus.None, null)]
    public void ShortStatus_PicksExactlyOneValue(TransferProgressShortStatus which, string? expected)
    {
        // 59,000 bytes still to go at 1024 B/s => 57s
        var snapshot = TestData.Snapshot(TestData.Result(
            transferred: 41_000,
            total: 100_000,
            bytesPerSecond: 1024
        ));

        Assert.Equal(expected, TransferProgressContentBuilder.BuildShortStatus(snapshot, new() { ShortStatus = which }));
    }


    [Fact]
    public void Data_IsOmittedWhenRawDataIsOff()
    {
        var content = TransferProgressContentBuilder.Build(TestData.Snapshot(), new() { IncludeRawData = false });
        Assert.Empty(content.Data);
    }


    [Fact]
    public void Data_CarriesTheMachineReadableValues()
    {
        var content = TransferProgressContentBuilder.Build(
            TestData.Snapshot(TestData.Result(transferred: 50, total: 100, bytesPerSecond: 5)),
            new()
        );

        Assert.Equal("50", content.Data["bytes"]);
        Assert.Equal("100", content.Data["total"]);
        Assert.Equal("0.5", content.Data["percent"]);
        Assert.Equal("5", content.Data["bps"]);
        Assert.Equal("10", content.Data["etaSeconds"]);
        Assert.Equal("download", content.Data["direction"]);
        Assert.Equal("one", content.Data["transferId"]);
        Assert.Equal("big.zip", content.Data["fileName"]);
    }


    [Fact]
    public void StaleDate_IsSetWhileRunningAndClearedWhenDone()
    {
        var options = new TransferProgressOptions { StaleAfter = TimeSpan.FromSeconds(30) };
        var now = DateTimeOffset.UtcNow;

        var running = TransferProgressContentBuilder.Build(TestData.Snapshot(), options, now: now);
        Assert.Equal(now.AddSeconds(30), running.StaleDate);

        var done = TransferProgressContentBuilder.Build(
            TestData.Snapshot(TestData.Result(status: HttpTransferState.Completed)), options, now: now
        );
        Assert.Null(done.StaleDate);
    }


    [Fact]
    public void Delegate_OverridesOnlyWhatItReturns()
    {
        var content = TransferProgressContentBuilder.Build(TestData.Snapshot(), new(), new TitleOnlyDelegate());

        Assert.Equal("Sending your photo", content.Title);
        Assert.Equal("custom", content.Data["tag"]);
        Assert.NotNull(content.Body);        // still the built-in body
    }


    class TitleOnlyDelegate : TransferProgressDelegate
    {
        public override string? GetTitle(TransferProgressSnapshot snapshot) => "Sending your photo";

        public override void OnContentBuilding(TransferProgressSnapshot snapshot, IDictionary<string, string> data)
            => data["tag"] = "custom";
    }
}
