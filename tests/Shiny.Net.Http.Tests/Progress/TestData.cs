using Xunit;

namespace Shiny.Net.Http.Tests;


static class TestData
{
    public static HttpTransferResult Result(
        string id = "one",
        TransferType type = TransferType.Download,
        string uri = "https://cdn.example.com/big.zip",
        string localPath = "/tmp/big.zip",
        HttpTransferState status = HttpTransferState.InProgress,
        long transferred = 0,
        long? total = 100,
        long bytesPerSecond = 0,
        Exception? exception = null
    ) => new(
        new HttpTransferRequest(id, uri, type, localPath),
        status,
        new TransferProgress(bytesPerSecond, total, transferred),
        exception
    );


    public static TransferProgressSnapshot Snapshot(
        HttpTransferResult? result = null,
        HttpTransferState? status = null
    )
    {
        var value = result ?? Result();
        return new TransferProgressSnapshot([value], status ?? value.Status, value.Progress, IsSummary: false);
    }
}
