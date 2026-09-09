using Microsoft.Extensions.Time.Testing;

namespace Shiny.Beacons.Tests;


public class EddystoneUrlCodecTests
{
    [Theory]
    [InlineData("http://www.example.com/", 0x00)]
    [InlineData("https://www.example.com/", 0x01)]
    [InlineData("http://example.com/", 0x02)]
    [InlineData("https://example.com/", 0x03)]
    public void Encode_PicksTheRightSchemeByte(string url, byte expected)
        => Assert.Equal(expected, EddystoneUrlCodec.Encode(url)[0]);


    [Theory]
    [InlineData("https://shinylib.net/")]
    [InlineData("https://www.google.com/")]
    [InlineData("http://example.org/x")]
    [InlineData("https://a.biz")]
    [InlineData("https://example.info/a/b")]
    [InlineData("https://example.gov")]
    [InlineData("http://www.a.edu/z")]
    public void EncodeDecode_RoundTrips(string url)
        => Assert.Equal(url, EddystoneUrlCodec.Decode(EddystoneUrlCodec.Encode(url)));


    [Fact]
    public void Encode_CompressesTheTopLevelDomain()
    {
        var encoded = EddystoneUrlCodec.Encode("https://example.com/");

        // scheme byte + "example" + one byte standing in for ".com/"
        Assert.Equal(1 + 7 + 1, encoded.Length);
        Assert.Equal(0x00, encoded[^1]);
    }


    [Fact]
    public void Encode_PrefersTheLongestExpansion()
    {
        // ".com/" must win over ".com", otherwise the trailing slash costs an extra byte
        var withSlash = EddystoneUrlCodec.Encode("https://a.com/");
        var without = EddystoneUrlCodec.Encode("https://a.com");

        Assert.Equal(withSlash.Length, without.Length);
        Assert.Equal(0x00, withSlash[^1]);
        Assert.Equal(0x07, without[^1]);
    }


    [Fact]
    public void Encode_PrefersTheLongestScheme()
    {
        // "http://www." must beat "http://", or the www is spelled out and wastes four bytes
        Assert.Equal(0x00, EddystoneUrlCodec.Encode("http://www.a.com")[0]);
    }


    [Fact]
    public void Encode_RejectsAnUnknownScheme()
        => Assert.Throws<ArgumentException>(() => EddystoneUrlCodec.Encode("ftp://example.com"));


    [Fact]
    public void Encode_RejectsAUrlThatIsTooLong()
        => Assert.Throws<ArgumentException>(() => EddystoneUrlCodec.Encode("https://averyveryverylongdomainname.com/andalongpath"));


    [Fact]
    public void Decode_RejectsAnUnknownSchemeByte()
        => Assert.Null(EddystoneUrlCodec.Decode([0x09, 0x61]));
}


public class EddystoneParserTests
{
    readonly FakeTimeProvider clock = new(DateTimeOffset.Parse("2026-09-09T12:00:00Z"));
    readonly BeaconRangingOptions options;

    public EddystoneParserTests()
        => this.options = new BeaconRangingOptions { TimeProvider = this.clock };


    [Fact]
    public void ServiceUuid_MatchesEveryPlatformSpelling()
    {
        // Apple hands back the short form, Android the lowercase long form, Windows/BlueZ uppercase
        Assert.True(EddystoneParser.IsEddystoneServiceUuid("FEAA"));
        Assert.True(EddystoneParser.IsEddystoneServiceUuid("feaa"));
        Assert.True(EddystoneParser.IsEddystoneServiceUuid("0000feaa-0000-1000-8000-00805f9b34fb"));
        Assert.True(EddystoneParser.IsEddystoneServiceUuid("0000FEAA-0000-1000-8000-00805F9B34FB"));

        Assert.False(EddystoneParser.IsEddystoneServiceUuid("FEAB"));
        Assert.False(EddystoneParser.IsEddystoneServiceUuid(null));
        Assert.False(EddystoneParser.IsEddystoneServiceUuid(""));
    }


    [Fact]
    public void Uid_IsParsed()
    {
        var uid = EddystoneUid.Parse("0102030405060708090A", "0B0C0D0E0F10");
        var payload = EddystoneBuilder.BuildUid(uid, -20);

        var frame = EddystoneParser.Parse(payload, "peripheral", -70, this.options);

        var uidFrame = Assert.IsType<EddystoneUidFrame>(frame);
        Assert.Equal(EddystoneFrameType.Uid, uidFrame.FrameType);
        Assert.Equal(uid, uidFrame.Uid);
        Assert.Equal("0102030405060708090A", uidFrame.Uid.Namespace);
        Assert.Equal("0B0C0D0E0F10", uidFrame.Uid.Instance);
        Assert.Equal(-20, uidFrame.TxPower);
        Assert.Equal("peripheral", uidFrame.PeripheralId);
        Assert.Equal(this.clock.GetUtcNow(), uidFrame.Timestamp);
    }


    [Fact]
    public void Url_IsParsed()
    {
        var payload = EddystoneBuilder.BuildUrl("https://shinylib.net/", -18);

        var frame = EddystoneParser.Parse(payload, "peripheral", -70, this.options);

        var urlFrame = Assert.IsType<EddystoneUrlFrame>(frame);
        Assert.Equal("https://shinylib.net/", urlFrame.Url);
        Assert.Equal(-18, urlFrame.TxPower);
    }


    [Fact]
    public void Tlm_IsParsed()
    {
        // version 0, 3.000 V, 25.5 C (0x19 0x80 in 8.8 fixed point), 1000 adverts, 100 seconds up
        byte[] payload =
        [
            0x20, 0x00,
            0x0B, 0xB8,
            0x19, 0x80,
            0x00, 0x00, 0x03, 0xE8,
            0x00, 0x00, 0x03, 0xE8
        ];

        var frame = EddystoneParser.Parse(payload, "peripheral", -70, this.options);

        var tlm = Assert.IsType<EddystoneTlmFrame>(frame);
        Assert.False(tlm.IsEncrypted);
        Assert.Equal(3.0, tlm.BatteryVolts);
        Assert.Equal(25.5, tlm.TemperatureCelsius);
        Assert.Equal(1000u, tlm.AdvertisementCount);
        Assert.Equal(TimeSpan.FromSeconds(100), tlm.Uptime);
    }


    [Fact]
    public void Tlm_NegativeTemperatureIsParsed()
    {
        // -10.5 C in 8.8 signed fixed point is 0xF580
        byte[] payload = [0x20, 0x00, 0x0B, 0xB8, 0xF5, 0x80, 0, 0, 0, 0, 0, 0, 0, 0];

        var tlm = Assert.IsType<EddystoneTlmFrame>(EddystoneParser.Parse(payload, "p", -70, this.options));
        Assert.Equal(-10.5, tlm.TemperatureCelsius);
    }


    [Fact]
    public void Tlm_UnsupportedSensorsAreNull()
    {
        // zero millivolts means mains powered, 0x8000 means no temperature sensor
        byte[] payload = [0x20, 0x00, 0x00, 0x00, 0x80, 0x00, 0, 0, 0, 0, 0, 0, 0, 0];

        var tlm = Assert.IsType<EddystoneTlmFrame>(EddystoneParser.Parse(payload, "p", -70, this.options));
        Assert.Null(tlm.BatteryVolts);
        Assert.Null(tlm.TemperatureCelsius);
    }


    [Fact]
    public void Tlm_EncryptedIsHandedBackRaw()
    {
        byte[] payload = [0x20, 0x01, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 0xAA, 0xBB, 0xCC, 0xDD];

        var tlm = Assert.IsType<EddystoneTlmFrame>(EddystoneParser.Parse(payload, "p", -70, this.options));
        Assert.True(tlm.IsEncrypted);
        Assert.Null(tlm.BatteryVolts);
        Assert.NotNull(tlm.EncryptedPayload);
        Assert.Equal(16, tlm.EncryptedPayload!.Length);
    }


    [Fact]
    public void Eid_IsSurfacedRaw()
    {
        byte[] payload = [0x30, 0xEE, 1, 2, 3, 4, 5, 6, 7, 8];

        var eid = Assert.IsType<EddystoneEidFrame>(EddystoneParser.Parse(payload, "p", -70, this.options));
        Assert.Equal(8, eid.EphemeralId.Length);
        Assert.Equal("0102030405060708", eid.EphemeralIdHex);
    }


    [Fact]
    public void UnknownFrameType_IsIgnored()
        => Assert.Null(EddystoneParser.Parse([0x40, 0x00, 0x01], "p", -70, this.options));


    [Fact]
    public void TruncatedPayloads_AreIgnored()
    {
        Assert.Null(EddystoneParser.Parse([], "p", -70, this.options));
        Assert.Null(EddystoneParser.Parse([0x00], "p", -70, this.options));
        Assert.Null(EddystoneParser.Parse([0x00, 0xEE, 0x01], "p", -70, this.options));
        Assert.Null(EddystoneParser.Parse([0x20, 0x00, 0x01], "p", -70, this.options));
    }


    [Fact]
    public void DistanceUsesTheZeroMetreCalibration()
    {
        // Eddystone calibrates at 0m and iBeacon at 1m, a 41 dBm difference. Without the shift,
        // every Eddystone beacon would read as far closer than it is.
        var payload = EddystoneBuilder.BuildUrl("https://a.com", -18);

        var frame = (EddystoneUrlFrame)EddystoneParser.Parse(payload, "p", -59, this.options)!;

        // -18 shifted to -59 equals the RSSI, so this should read as one metre
        Assert.Equal(1.0, frame.Distance, 2);
        Assert.Equal(Proximity.Near, frame.Proximity);
    }
}


public class EddystoneUidTests
{
    [Fact]
    public void Parse_RoundTrips()
    {
        var uid = EddystoneUid.Parse("0102030405060708090A", "0B0C0D0E0F10");

        Assert.Equal("0102030405060708090A", uid.Namespace);
        Assert.Equal("0B0C0D0E0F10", uid.Instance);
        Assert.Equal("0102030405060708090A:0B0C0D0E0F10", uid.ToString());
    }


    [Fact]
    public void EqualityIsByValue()
    {
        var a = EddystoneUid.Parse("0102030405060708090A", "0B0C0D0E0F10");
        var b = EddystoneUid.Parse("0102030405060708090A", "0B0C0D0E0F10");
        var c = EddystoneUid.Parse("0102030405060708090A", "0B0C0D0E0F11");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
    }


    [Fact]
    public void RejectsWrongLengths()
    {
        Assert.Throws<ArgumentException>(() => new EddystoneUid(new byte[9], new byte[6]));
        Assert.Throws<ArgumentException>(() => new EddystoneUid(new byte[10], new byte[5]));
    }


    [Fact]
    public void DefaultInstance_IsAllZeroesAndDoesNotThrow()
    {
        var uid = default(EddystoneUid);

        Assert.Equal("00000000000000000000", uid.Namespace);
        Assert.Equal("000000000000", uid.Instance);
    }
}
