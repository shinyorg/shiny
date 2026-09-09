using System.Buffers.Binary;
using Shiny.BluetoothLE;

namespace Shiny.Beacons.Tests;


public class IBeaconPacketTests
{
    // A real iBeacon payload: 0x02 0x15, then the UUID in wire (big-endian) order, major 1, minor 2,
    // measured power -59.
    static readonly byte[] KnownPacket =
    [
        0x02, 0x15,
        0xE2, 0xC5, 0x6D, 0xB5, 0xDF, 0xFB, 0x48, 0xD2,
        0xB0, 0x60, 0xD0, 0xF5, 0xA7, 0x10, 0x96, 0xE0,
        0x00, 0x01,
        0x00, 0x02,
        0xC5
    ];

    static readonly Guid KnownUuid = new("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0");


    [Fact]
    public void Read_DecodesKnownPacket()
    {
        var result = IBeaconPacket.Read(KnownPacket);

        Assert.NotNull(result);
        Assert.Equal(KnownUuid, result!.Value.Uuid);
        Assert.Equal(1, result.Value.Major);
        Assert.Equal(2, result.Value.Minor);
        Assert.Equal(-59, result.Value.TxPower);
    }


    [Fact]
    public void Build_ProducesTheSameBytesAsAKnownPacket()
    {
        // This is the regression guard for the endianness bug: building with Guid.ToByteArray() and
        // BitConverter.GetBytes() produced a byte-swapped UUID, major and minor, and no receiver
        // could match the beacon.
        var built = IBeaconPacket.Build(KnownUuid, 1, 2, -59);

        Assert.Equal(KnownPacket, built);
    }


    [Fact]
    public void Build_UuidIsBigEndian()
    {
        var built = IBeaconPacket.Build(KnownUuid, 0, 0);

        // the first byte of the UUID on the wire is the most significant one, not the least
        Assert.Equal(0xE2, built[2]);
        Assert.Equal(0xE0, built[17]);
    }


    [Theory]
    [InlineData((ushort)0, (ushort)0)]
    [InlineData((ushort)1, (ushort)2)]
    [InlineData((ushort)258, (ushort)772)]
    [InlineData((ushort)65535, (ushort)65535)]
    public void BuildRead_RoundTrips(ushort major, ushort minor)
    {
        var built = IBeaconPacket.Build(KnownUuid, major, minor, -70);
        var read = IBeaconPacket.Read(built);

        Assert.NotNull(read);
        Assert.Equal(KnownUuid, read!.Value.Uuid);
        Assert.Equal(major, read.Value.Major);
        Assert.Equal(minor, read.Value.Minor);
        Assert.Equal(-70, read.Value.TxPower);
    }


    [Fact]
    public void Build_MajorAndMinorAreBigEndian()
    {
        var built = IBeaconPacket.Build(KnownUuid, 0x0102, 0x0304);

        Assert.Equal(0x0102, BinaryPrimitives.ReadUInt16BigEndian(built.AsSpan(18, 2)));
        Assert.Equal(0x01, built[18]);
        Assert.Equal(0x02, built[19]);
        Assert.Equal(0x03, built[20]);
        Assert.Equal(0x04, built[21]);
    }


    [Fact]
    public void IsIBeacon_RequiresAppleCompanyId()
    {
        // The pre-revival parser accepted any manufacturer payload starting 0x02 0x15, whoever sent it
        Assert.True(IBeaconPacket.IsIBeacon(0x004C, KnownPacket));
        Assert.False(IBeaconPacket.IsIBeacon(0x0059, KnownPacket));
        Assert.True(IBeaconPacket.IsIBeacon(KnownPacket));
    }


    [Fact]
    public void IsIBeacon_RejectsShortOrWrongHeaders()
    {
        Assert.False(IBeaconPacket.IsIBeacon([]));
        Assert.False(IBeaconPacket.IsIBeacon(KnownPacket.AsSpan(0, 10)));

        var wrongType = KnownPacket.ToArray();
        wrongType[0] = 0xBE;
        Assert.False(IBeaconPacket.IsIBeacon(wrongType));

        var wrongLength = KnownPacket.ToArray();
        wrongLength[1] = 0x16;
        Assert.False(IBeaconPacket.IsIBeacon(wrongLength));
    }


    [Fact]
    public void Read_TolerantOfTrailingBytes()
    {
        // some beacons pad the advertisement out past the 23 bytes iBeacon defines
        var padded = KnownPacket.Concat(new byte[] { 0x00, 0x00 }).ToArray();

        var result = IBeaconPacket.Read(padded);
        Assert.NotNull(result);
        Assert.Equal(KnownUuid, result!.Value.Uuid);
    }


    [Fact]
    public void Write_RejectsUndersizedBuffer()
        => Assert.Throws<ArgumentException>(() => IBeaconPacket.Write(new byte[22], KnownUuid, 1, 2));
}
