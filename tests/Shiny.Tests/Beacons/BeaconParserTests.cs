using System;
using Shiny.Beacons;
using FluentAssertions;
using Xunit;


namespace Shiny.Tests.Beacons
{
    public class BeaconParserTests
    {
        [Theory]
        [InlineData(
            "4C0002159DD3C2D7C05E417492D2CC30629E522700010001C6",
            "9dd3c2d7-c05e-4174-92d2-cc30629e5227",
            1,
            1,
            Proximity.Immediate
        )]
        public void ParseBeaconPacketSuccess(string hexData, string beaconIdentifier, ushort major, ushort minor, Proximity prox)
        {
            var bytes = hexData.FromHex();
            var beacon = Beacon.Parse(bytes, 10);
            beacon.Uuid.Should().Be(new Guid(beaconIdentifier));
            beacon.Major.Should().Be(major);
            beacon.Minor.Should().Be(minor);
            beacon.Proximity.Should().Be(prox);
        }


        [Theory]
        [InlineData("4C0002159DD3C2D7C05E417492D2CC30629E522700010001C6", true)]
        public void DetectBeacon(string hexData, bool expectedResult)
        {
            var bytes = hexData.FromHex();
            Beacon.IsIBeaconPacket(bytes).Should().Be(expectedResult);
        }


        [Fact]
        public void ToBeaconIsBeacon()
        {
            var beacon = new Beacon(Guid.NewGuid(), 99, 199, 0, Proximity.Far);
            var bytes = beacon.ToIBeaconPacket();
            Beacon.IsIBeaconPacket(bytes).Should().Be(true);
        }


        [Fact]
        public void ToBeacon()
        {
            var beacon = new Beacon(Guid.NewGuid(), 99, 199, 0, Proximity.Far);
            var bytes = beacon.ToIBeaconPacket();
            var beacon2 = Beacon.Parse(bytes, 0);
            beacon.Uuid.Should().Be(beacon2.Uuid);
            beacon.Major.Should().Be(beacon2.Major);
            beacon.Minor.Should().Be(beacon2.Minor);
        }
    }
}
