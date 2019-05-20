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
            "02-15-B9-40-7F-30-F5-F8-46-6E-AF-F9-25-55-6B-57-FE-6D-39-B0-57-CA-BE",
            "B9407F30-F5F8-466E-AFF9-25556B57FE6D",
            1,
            1,
            Proximity.Immediate
        )]
        public void ParseBeaconPacketSuccess(string hexData, string beaconIdentifier, ushort major, ushort minor, Proximity prox)
        {
            var bytes = hexData.FromHex();
            Beacon.IsBeaconPacket(bytes).Should().BeTrue();
            var beacon = Beacon.Parse(bytes, 10);
            beacon.Uuid.Should().Be(new Guid(beaconIdentifier));
            beacon.Major.Should().Be(major);
            beacon.Minor.Should().Be(minor);
            beacon.Proximity.Should().Be(prox);
        }


        [Fact]
        public void ToBeaconIsBeacon()
        {
            var beacon = new Beacon(Guid.NewGuid(), 99, 199, 0, Proximity.Far);
            var bytes = beacon.ToIBeaconPacket();
            Beacon.IsBeaconPacket(bytes).Should().Be(true);
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
