using Shiny.Beacons;

namespace Shiny.Tests.Beacons;


public class BeaconParserTests
{
    [Theory]
    [InlineData(
        "02-15-B9-40-7F-30-F5-F8-46-6E-AF-F9-25-55-6B-57-FE-6D-39-B0-57-CA-BE",
        "B9407F30-F5F8-466E-AFF9-25556B57FE6D",
        14768,
        22474,
        Proximity.Immediate
    )]
    [InlineData(
        "02-15-A7-AE-2E-B7-1F-00-41-68-B9-9B-A7-49-BA-C1-CA-64-00-01-00-40-C5-58",
        "A7AE2EB7-1F00-4168-B99B-A749BAC1CA64",
        1,
        64,
        Proximity.Immediate
    )]
    [InlineData(
        "02-15-F7-82-6D-A6-4F-A2-4E-98-80-24-BC-5B-71-E0-89-3E-04-56-00-10-BF-3E",
        "f7826da6-4fa2-4e98-8024-bc5b71e0893e",
        1110,
        16,
        Proximity.Immediate
    )]
    [InlineData(
        "02-15-F7-82-6D-A6-4F-A2-4E-98-80-24-BC-5B-71-E0-89-3E-04-56-00-10-BF",
        "f7826da6-4fa2-4e98-8024-bc5b71e0893e",
        1110,
        16,
        Proximity.Immediate
    )]
    public void ParseBeaconPacketSuccess(string hexData, string beaconIdentifier, ushort major, ushort minor, Proximity prox)
    {
        var bytes = hexData.FromHex();
        bytes.IsBeaconPacket().Should().BeTrue();
        var beacon = bytes.Parse(10);
        beacon.Uuid.Should().Be(new Guid(beaconIdentifier));
        beacon.Major.Should().Be(major);
        beacon.Minor.Should().Be(minor);
        beacon.Proximity.Should().Be(prox);
    }


    //[Fact]
    //public void ToBeaconIsBeacon()
    //{
    //    var beacon = new Beacon(Guid.NewGuid(), 99, 199, Proximity.Far);
    //    var bytes = beacon.ToIBeaconPacket();
    //    bytes.IsBeaconPacket().Should().Be(true);
    //}


    //[Fact]
    //public void ToBeacon()
    //{
    //    var beacon = new Beacon(Guid.NewGuid(), 99, 199, Proximity.Far);
    //    var bytes = beacon.ToIBeaconPacket();
    //    var beacon2 = bytes.Parse(0);
    //    beacon.Uuid.Should().Be(beacon2.Uuid);
    //    beacon.Major.Should().Be(beacon2.Major);
    //    beacon.Minor.Should().Be(beacon2.Minor);
    //}
}
