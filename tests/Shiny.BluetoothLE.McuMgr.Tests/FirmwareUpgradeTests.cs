namespace Shiny.BluetoothLE.McuMgr.Tests;


public class FirmwareUpgradeTests
{
    static FakeSmpDevice DeviceRunning(byte[] runningImage)
    {
        var device = new FakeSmpDevice();
        var parsed = McuBootImage.Parse(runningImage);
        device.Slots.Add(new FakeSmpDevice.Slot(0, 0, parsed.Hash, parsed.Version.ToString()) { Active = true, Confirmed = true });
        return device;
    }


    [Fact]
    public async Task TestAndConfirm_UploadsSwapsAndConfirms()
    {
        var current = TestImages.Create(800, new McuBootVersion(1, 0, 0, 0), 1);
        var update = TestImages.Create(3000, new McuBootVersion(1, 1, 0, 0), 2);
        var device = DeviceRunning(current);
        var client = new McuMgrClient(device);
        var stages = new List<FirmwareUpgradeStage>();

        var result = await client.UpgradeFirmwareAsync(
            new FirmwareImage(update),
            new FirmwareUpgradeOptions { EstimatedSwapTime = TimeSpan.Zero },
            new SyncProgress<FirmwareUpgradeProgress>(x => stages.Add(x.Stage))
        );

        Assert.Equal([0], result.UploadedImages);
        Assert.True(result.DeviceReset);
        Assert.Equal(1, device.Resets);
        Assert.Equal(1, device.Reconnects);

        var running = device.Slots.Single(x => x.SlotIndex == 0);
        Assert.Equal("1.1.0", running.Version);
        Assert.True(running.Active);
        Assert.True(running.Confirmed);

        Assert.Equal(
            new[]
            {
                FirmwareUpgradeStage.Validating,
                FirmwareUpgradeStage.Uploading,
                FirmwareUpgradeStage.Testing,
                FirmwareUpgradeStage.Resetting,
                FirmwareUpgradeStage.Reconnecting,
                FirmwareUpgradeStage.Confirming,
                FirmwareUpgradeStage.Completed
            },
            stages.Distinct()
        );

        // the test request must not be permanent - only the post-swap confirm makes it stick
        var stateWrites = device.Requests.Where(x => x.Header is { Group: McuMgrGroups.Image, Command: 0, Operation: SmpOperation.Write }).ToList();
        Assert.False(stateWrites[0].Payload.GetBoolean("confirm"));
        Assert.True(stateWrites[1].Payload.GetBoolean("confirm"));
    }


    [Fact]
    public async Task AlreadyRunning_SkipsEverything()
    {
        var current = TestImages.Create(800, new McuBootVersion(1, 0, 0, 0), 1);
        var device = DeviceRunning(current);
        var client = new McuMgrClient(device);

        var result = await client.UpgradeFirmwareAsync(new FirmwareImage(current));

        Assert.True(result.AlreadyUpToDate);
        Assert.Equal([0], result.SkippedImages);
        Assert.Equal(0, device.Resets);
        Assert.Empty(device.Uploads);
    }


    [Fact]
    public async Task AlreadyUploaded_SkipsUploadButSwaps()
    {
        var current = TestImages.Create(800, new McuBootVersion(1, 0, 0, 0), 1);
        var update = TestImages.Create(900, new McuBootVersion(1, 1, 0, 0), 2);
        var device = DeviceRunning(current);
        var staged = McuBootImage.Parse(update);
        device.Slots.Add(new FakeSmpDevice.Slot(0, 1, staged.Hash, staged.Version.ToString()));
        var client = new McuMgrClient(device);

        var result = await client.UpgradeFirmwareAsync(new FirmwareImage(update), new FirmwareUpgradeOptions { EstimatedSwapTime = TimeSpan.Zero });

        Assert.Empty(result.UploadedImages);
        Assert.True(result.DeviceReset);
        Assert.Empty(device.Uploads);
        Assert.Equal("1.1.0", device.Slots.Single(x => x.SlotIndex == 0).Version);
    }


    [Fact]
    public async Task Reverted_Throws()
    {
        var current = TestImages.Create(800, new McuBootVersion(1, 0, 0, 0), 1);
        var update = TestImages.Create(900, new McuBootVersion(1, 1, 0, 0), 2);
        var device = DeviceRunning(current);
        device.RevertOnSwap = true;
        var client = new McuMgrClient(device);

        await Assert.ThrowsAsync<McuMgrException>(() =>
            client.UpgradeFirmwareAsync(new FirmwareImage(update), new FirmwareUpgradeOptions { EstimatedSwapTime = TimeSpan.Zero })
        );
    }


    [Fact]
    public async Task UploadOnly_DoesNotReset()
    {
        var device = DeviceRunning(TestImages.Create(800, new McuBootVersion(1, 0, 0, 0), 1));
        var client = new McuMgrClient(device);

        var result = await client.UpgradeFirmwareAsync(
            new FirmwareImage(TestImages.Create(900, new McuBootVersion(1, 1, 0, 0), 2)),
            new FirmwareUpgradeOptions { Mode = FirmwareUpgradeMode.UploadOnly }
        );

        Assert.False(result.DeviceReset);
        Assert.Equal(0, device.Resets);
        Assert.Contains(device.Slots, x => x.SlotIndex == 1 && x.Version == "1.1.0" && !x.Pending);
    }


    [Fact]
    public async Task ConfirmOnly_MarksPermanent()
    {
        var device = DeviceRunning(TestImages.Create(800, new McuBootVersion(1, 0, 0, 0), 1));
        var client = new McuMgrClient(device);

        await client.UpgradeFirmwareAsync(
            new FirmwareImage(TestImages.Create(900, new McuBootVersion(1, 1, 0, 0), 2)),
            new FirmwareUpgradeOptions { Mode = FirmwareUpgradeMode.ConfirmOnly, EstimatedSwapTime = TimeSpan.Zero }
        );

        var running = device.Slots.Single(x => x.SlotIndex == 0);
        Assert.Equal("1.1.0", running.Version);
        Assert.True(running.Confirmed);
    }


    [Fact]
    public async Task MultiImage_UploadsEachToItsImageIndex()
    {
        var device = DeviceRunning(TestImages.Create(800, new McuBootVersion(1, 0, 0, 0), 1));
        var client = new McuMgrClient(device);
        var app = TestImages.Create(900, new McuBootVersion(1, 1, 0, 0), 2);
        var net = TestImages.Create(700, new McuBootVersion(1, 1, 0, 0), 3);

        var result = await client.UpgradeFirmwareAsync(
            [new FirmwareImage(app, 0), new FirmwareImage(net, 1)],
            new FirmwareUpgradeOptions { EstimatedSwapTime = TimeSpan.Zero }
        );

        Assert.Equal([0, 1], result.UploadedImages);
        Assert.Equal(app, device.Uploads[0].ToArray());
        Assert.Equal(net, device.Uploads[1].ToArray());

        var firstChunks = device.Requests
            .Where(x => x.Header.Group == McuMgrGroups.Image && x.Header.Command == 1 && x.Payload.GetInt64("off") == 0)
            .Select(x => x.Payload.GetInt64("image") ?? 0)
            .ToList();
        Assert.Equal([0L, 1L], firstChunks);
    }
}
