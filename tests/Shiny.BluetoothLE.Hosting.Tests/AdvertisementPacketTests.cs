using Shiny.BluetoothLE.Hosting.Infrastructure;
using Xunit;

namespace Shiny.BluetoothLE.Hosting.Tests;


public class AdvertisementPacketTests
{
    const string Uuid128A = "A495FF20-C5B1-4B44-B512-1370F02D74DE";
    const string Uuid128B = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";

    // Apple and Windows always send flags; Android and BlueZ send them on connectable advertisements
    static readonly PacketLayout Apple = new(Flags: true, TxPower: false);
    static readonly PacketLayout Windows = new(Flags: true, TxPower: false);
    static readonly PacketLayout AndroidConnectable = new(Flags: true, TxPower: true);
    static readonly PacketLayout AndroidBroadcast = new(Flags: false, TxPower: true);


    [Fact]
    public void TwoCustomServiceUuids_Throw()
    {
        var options = new AdvertisementOptions("Shiny", Uuid128A, Uuid128B);

        // flags 3 + one 128-bit list holding both UUIDs (2 + 32)
        var ex = Assert.Throws<ArgumentException>(() => AdvertisementPacket.EnsureFits(options, Apple));
        Assert.Equal("options", ex.ParamName);
        Assert.Contains("needs 37 bytes", ex.Message);
        Assert.Contains("2 128-bit service UUIDs 34", ex.Message);
    }


    [Fact]
    public void TwoCustomServiceUuids_ThrowEvenWithoutFlags()
    {
        var options = new AdvertisementOptions(null, Uuid128A, Uuid128B) { IsConnectable = false };
        Assert.Throws<ArgumentException>(() => AdvertisementPacket.EnsureFits(options, AndroidBroadcast));
    }


    [Fact]
    public void OneCustomServiceUuidAndName_Fit()
    {
        // the name is never counted - the platforms move it to the scan response or truncate it
        var options = new AdvertisementOptions("A Rather Long Local Name For A Peripheral", Uuid128A);
        AdvertisementPacket.EnsureFits(options, Windows);
        AdvertisementPacket.EnsureFits(options, Apple);
    }


    [Fact]
    public void CustomAndSigUuids_Fit()
    {
        // flags 3 + 128-bit list 18 + 16-bit list holding two (2 + 4) = 27
        var options = new AdvertisementOptions(null, Uuid128A, "180D", "0000180F-0000-1000-8000-00805F9B34FB");
        var sections = AdvertisementPacket.Measure(options, Apple);

        Assert.Equal(27, sections.Sum(x => x.Length));
        AdvertisementPacket.EnsureFits(options, Apple);
    }


    [Theory]
    [InlineData("180D", 2)]
    [InlineData("180d", 2)]
    [InlineData("0000180D", 2)]
    [InlineData("0000180D-0000-1000-8000-00805F9B34FB", 2)]
    [InlineData("0000feaa-0000-1000-8000-00805f9b34fb", 2)]
    [InlineData("12345678", 4)]
    [InlineData("12345678-0000-1000-8000-00805F9B34FB", 4)]
    [InlineData(Uuid128A, 16)]
    [InlineData("not-a-uuid", 16)]
    public void UuidLength(string uuid, int expected)
        => Assert.Equal(expected, AdvertisementPacket.GetUuidLength(uuid));


    [Fact]
    public void LargestEddystoneFrame_FitsExactlyWithFlags()
    {
        // Eddystone-UID / URL frames are at most 20 bytes: flags 3 + FEAA list 4 + service data (2 + 2 + 20) = 31
        var options = new AdvertisementOptions(null, "FEAA")
        {
            ServiceData = [new AdvertisementServiceData("FEAA", new byte[20])],
            IsConnectable = false
        };

        Assert.Equal(AdvertisementPacket.MaxLength, AdvertisementPacket.Measure(options, Windows).Sum(x => x.Length));
        AdvertisementPacket.EnsureFits(options, Windows);
    }


    [Fact]
    public void ManufacturerData_CountsCompanyIdAndPayload()
    {
        // flags 3 + 128-bit UUID 18 + manufacturer data (2 + 2 + 6) = 31
        var fits = new AdvertisementOptions(null, Uuid128A) { ManufacturerData = new ManufacturerData(0x004C, new byte[6]) };
        AdvertisementPacket.EnsureFits(fits, AndroidConnectable);

        var tooBig = fits with { ManufacturerData = new ManufacturerData(0x004C, new byte[7]) };
        var ex = Assert.Throws<ArgumentException>(() => AdvertisementPacket.EnsureFits(tooBig, AndroidConnectable));
        Assert.Contains("manufacturer data 11", ex.Message);
    }


    [Fact]
    public void TxPower_CountsOnlyWhereThePlatformSendsIt()
    {
        // flags 3 + 128-bit UUID 18 + manufacturer data (2 + 2 + 6) = 31, + tx power 3 on Android
        var options = new AdvertisementOptions(null, Uuid128A)
        {
            ManufacturerData = new ManufacturerData(0x004C, new byte[6]),
            IncludeTxPower = true
        };

        AdvertisementPacket.EnsureFits(options, Windows);
        var ex = Assert.Throws<ArgumentException>(() => AdvertisementPacket.EnsureFits(options, AndroidConnectable));
        Assert.Contains("tx power 3", ex.Message);
    }


    [Fact]
    public void EmptyOptions_Fit()
        => Assert.Equal(3, AdvertisementPacket.Measure(new AdvertisementOptions(), Apple).Sum(x => x.Length));
}
