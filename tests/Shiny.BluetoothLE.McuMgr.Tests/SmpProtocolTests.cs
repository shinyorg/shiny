namespace Shiny.BluetoothLE.McuMgr.Tests;


public class SmpProtocolTests
{
    [Fact]
    public void Header_RoundTrips_WithSpecBitLayout()
    {
        var header = new SmpHeader(SmpOperation.Write, SmpVersion.V2, 0, 0x0102, McuMgrGroups.Image, 7, 1);
        var bytes = new byte[8];
        header.Write(bytes);

        // version in bits 3-4, op in bits 0-2, big endian length and group
        Assert.Equal(new byte[] { 0b0000_1010, 0, 0x01, 0x02, 0x00, 0x01, 7, 1 }, bytes);
        Assert.Equal(header, SmpHeader.Read(bytes));
        Assert.Equal(8 + 0x0102, SmpHeader.GetFrameLength(bytes));
        Assert.Null(SmpHeader.GetFrameLength(bytes.AsSpan(0, 7)));
    }


    [Fact]
    public void Cbor_RoundTrips_NestedValues()
    {
        var payload = new Dictionary<string, object?>
        {
            ["off"] = 1234,
            ["neg"] = -5,
            ["name"] = "file.txt",
            ["data"] = new byte[] { 1, 2, 3 },
            ["confirm"] = true,
            ["nothing"] = null,
            ["argv"] = new List<object?> { "kernel", "uptime" },
            ["err"] = new Dictionary<string, object?> { ["group"] = 1, ["rc"] = 2 }
        };

        var decoded = SmpCbor.Decode(SmpCbor.Encode(payload));

        Assert.Equal(1234, decoded.GetInt64("off"));
        Assert.Equal(-5, decoded.GetInt64("neg"));
        Assert.Equal("file.txt", decoded.GetString("name"));
        Assert.Equal(new byte[] { 1, 2, 3 }, decoded.GetBytes("data"));
        Assert.True(decoded.GetBoolean("confirm"));
        Assert.Null(decoded["nothing"]);
        Assert.Equal(new object?[] { "kernel", "uptime" }, decoded.GetList("argv"));
        Assert.Equal(2, decoded.GetMap("err")!.GetInt64("rc"));
    }


    [Fact]
    public void Cbor_Decodes_IndefiniteLengthMaps()
    {
        // tinycbor-era firmware: {_ "rc": 0, "r": "hi"}
        var bytes = new byte[] { 0xBF, 0x62, (byte)'r', (byte)'c', 0x00, 0x61, (byte)'r', 0x62, (byte)'h', (byte)'i', 0xFF };
        var decoded = SmpCbor.Decode(bytes);

        Assert.Equal(0, decoded.GetInt64("rc"));
        Assert.Equal("hi", decoded.GetString("r"));
    }


    [Theory]
    [InlineData(64)]
    [InlineData(247)]
    [InlineData(384)]
    [InlineData(2475)]
    public void ChunkLength_FillsButNeverExceedsFrame(int frameLimit)
    {
        var request = new Dictionary<string, object?> { ["off"] = 70000L };
        var length = ChunkedUpload.ComputeChunkLength(request, 1_000_000, frameLimit, 1);

        request["data"] = new byte[length];
        var frame = SmpHeader.Size + SmpCbor.Encode(request).Length;
        Assert.True(frame <= frameLimit, $"frame {frame} > {frameLimit}");

        request["data"] = new byte[length + 1];
        Assert.True(SmpHeader.Size + SmpCbor.Encode(request).Length > frameLimit);
    }


    [Fact]
    public void ChunkLength_RespectsAlignment_ExceptForLastChunk()
    {
        var aligned = ChunkedUpload.ComputeChunkLength(new() { ["off"] = 100L }, 10_000, 247, 16);
        Assert.Equal(0, aligned % 16);

        var last = ChunkedUpload.ComputeChunkLength(new() { ["off"] = 100L }, 13, 247, 16);
        Assert.Equal(13, last);
    }


    [Fact]
    public void McuBootImage_ParsesVersionAndHash()
    {
        var data = TestImages.Create(1000, new McuBootVersion(1, 2, 3, 0));
        var image = McuBootImage.Parse(data);

        Assert.Equal("1.2.3", image.Version.ToString());
        Assert.Equal(1000u, image.ImageSize);
        Assert.Equal(32, image.Hash.Length);
        Assert.Equal(System.Security.Cryptography.SHA256.HashData(data.AsSpan(0, 1032)), image.Hash);
    }


    [Fact]
    public void McuBootImage_SkipsProtectedTlvs()
    {
        var data = TestImages.Create(500, new McuBootVersion(2, 0, 1, 7), withProtectedTlv: true);
        var image = McuBootImage.Parse(data);

        Assert.Equal("2.0.1.7", image.Version.ToString());
        Assert.Equal(System.Security.Cryptography.SHA256.HashData(data.AsSpan(0, 32 + 500 + 12)), image.Hash);
    }


    [Fact]
    public void McuBootImage_RejectsUnsignedBinary()
        => Assert.Throws<InvalidDataException>(() => McuBootImage.Parse(new byte[256]));


    [Fact]
    public async Task DfuPackage_ReadsImagesAndIndexes()
    {
        var app = TestImages.Create(300, new McuBootVersion(1, 0, 0, 0), 1);
        var net = TestImages.Create(200, new McuBootVersion(1, 0, 0, 0), 2);
        var zip = TestImages.CreateDfuZip(("app.bin", "0", app, "application"), ("net.bin", "1", net, "application"));

        var images = await FirmwareImage.FromDfuPackageAsync(new MemoryStream(zip));

        Assert.Equal(2, images.Count);
        Assert.Equal(0, images[0].ImageIndex);
        Assert.Equal(app, images[0].Data);
        Assert.Equal(1, images[1].ImageIndex);
        Assert.Equal(net, images[1].Data);
    }


    [Fact]
    public async Task DfuPackage_RejectsSuit()
    {
        var zip = TestImages.CreateDfuZip(("root.suit", "0", new byte[10], "suit-envelope"));
        await Assert.ThrowsAsync<NotSupportedException>(() => FirmwareImage.FromDfuPackageAsync(new MemoryStream(zip)));
    }
}
