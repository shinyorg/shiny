namespace Shiny.BluetoothLE.McuMgr.Tests;


public class McuMgrClientTests
{
    [Fact]
    public async Task Echo_RoundTrips()
    {
        var device = new FakeSmpDevice();
        var client = new McuMgrClient(device);

        Assert.Equal("hello", await client.Os.EchoAsync("hello"));
        Assert.Equal(SmpVersion.V2, device.Requests[0].Header.Version);
    }


    [Fact]
    public async Task V1Error_ThrowsWithManagementError()
    {
        var device = new FakeSmpDevice();
        var client = new McuMgrClient(device);

        var ex = await Assert.ThrowsAsync<McuMgrException>(() => client.Files.GetSizeAsync("/missing"));
        Assert.Equal(McuMgrError.NotSupported, ex.Error);
        Assert.Equal(McuMgrGroups.FileSystem, ex.Group);
    }


    [Fact]
    public async Task V2GroupError_ThrowsWithGroupCode()
    {
        var device = new FakeSmpDevice();
        var client = new McuMgrClient(device);

        var ex = await Assert.ThrowsAsync<McuMgrException>(() => client.Images.SetStateAsync(new byte[32], false));
        Assert.Null(ex.Error);
        Assert.Equal(McuMgrGroups.Image, ex.Group);
        Assert.Equal(8, ex.GroupErrorCode);
    }


    [Fact]
    public async Task V1OnlyDevice_DowngradesAndRetries()
    {
        var device = new FakeSmpDevice { SupportsV2 = false };
        var client = new McuMgrClient(device);

        Assert.Equal("x", await client.Os.EchoAsync("x"));
        Assert.Equal(SmpVersion.V1, client.Version);
        Assert.Equal(2, device.Requests.Count);
        Assert.Equal(SmpVersion.V1, device.Requests[1].Header.Version);

        await client.Os.EchoAsync("y");
        Assert.Equal(3, device.Requests.Count); // no second negotiation
    }


    [Fact]
    public async Task Timeout_ThrowsTimeoutException()
    {
        var client = new McuMgrClient(new HangingTransport(), new McuMgrClientOptions { RequestTimeout = TimeSpan.FromMilliseconds(50) });
        await Assert.ThrowsAsync<TimeoutException>(() => client.Os.EchoAsync("x"));
    }


    [Fact]
    public async Task ImageUpload_ChunksWithinFrameLimit_AndReassemblesExactly()
    {
        var device = new FakeSmpDevice { MaxFrameSize = 128 };
        var client = new McuMgrClient(device);
        var image = TestImages.Create(5000, new McuBootVersion(1, 0, 0, 0));
        var reports = new List<TransferProgress>();

        await client.Images.UploadAsync(image, progress: new SyncProgress<TransferProgress>(reports.Add));

        Assert.Equal(image, device.Uploads[0].ToArray());
        Assert.All(device.SentFrames, x => Assert.True(x.Length <= 128, $"frame of {x.Length}"));

        var uploads = device.Requests.Where(x => x.Header.Group == McuMgrGroups.Image && x.Header.Command == 1).ToList();
        Assert.NotNull(uploads[0].Payload.GetBytes("sha"));
        Assert.All(uploads.Skip(1), x => Assert.Null(x.Payload.GetBytes("sha")));
        Assert.Equal(1.0, reports[^1].PercentComplete);
    }


    [Fact]
    public async Task ImageUpload_UsesDeviceBufferSize_WhenReported()
    {
        var device = new FakeSmpDevice { MaxFrameSize = 128, BufferSize = 1024 };
        var client = new McuMgrClient(device);

        await client.Images.UploadAsync(TestImages.Create(5000, new McuBootVersion(1, 0, 0, 0)));

        var largest = device.SentFrames.Max(x => x.Length);
        Assert.True(largest is > 128 and <= 1024, $"largest frame {largest}");
    }


    [Fact]
    public async Task ImageUpload_ResumesFromDeviceOffset()
    {
        var image = TestImages.Create(4000, new McuBootVersion(1, 0, 0, 0));
        // the device already holds the first 3000 bytes from an interrupted attempt
        var device = new FakeSmpDevice { PartialUpload = image[..3000] };
        var client = new McuMgrClient(device);

        await client.Images.UploadAsync(image);

        var offsets = device.Requests
            .Where(x => x.Header.Group == McuMgrGroups.Image && x.Header.Command == 1)
            .Select(x => x.Payload.GetInt64("off"))
            .ToList();

        Assert.Equal(0, offsets[0]);
        Assert.Equal(3000, offsets[1]);
        Assert.Equal(image, device.Uploads[0].ToArray());
    }


    [Fact]
    public async Task FileSystem_UploadThenDownload()
    {
        var device = new FakeSmpDevice();
        var client = new McuMgrClient(device);
        var data = new byte[777];
        new Random(3).NextBytes(data);

        await client.Files.UploadAsync("/lfs/a.bin", data);
        Assert.Equal(data, device.Files["/lfs/a.bin"]);

        var downloaded = await client.Files.DownloadAsync("/lfs/a.bin");
        Assert.Equal(data, downloaded);
    }


    [Fact]
    public async Task ShellAndStats_Parse()
    {
        var client = new McuMgrClient(new FakeSmpDevice());

        var shell = await client.Shell.ExecuteAsync("kernel", "uptime");
        Assert.Equal("kernel uptime", shell.Output);

        Assert.Equal(["smp_svr_stats"], await client.Stats.ListGroupsAsync());
        Assert.Equal(42, (await client.Stats.ReadAsync("smp_svr_stats"))["ticks"]);
    }


    sealed class HangingTransport : ISmpTransport
    {
        public int MaxFrameSize => 247;
        public async Task<byte[]> SendAsync(byte[] frame, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return [];
        }
        public Task ReconnectAsync(TimeSpan delay, TimeSpan timeout, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}


sealed class SyncProgress<T>(Action<T> action) : IProgress<T>
{
    public void Report(T value) => action(value);
}
