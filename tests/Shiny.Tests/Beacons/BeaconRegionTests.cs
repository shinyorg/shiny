using Shiny.Beacons;

namespace Shiny.Tests.Beacons;


public class BeaconRegionTests
{
    const string Uuid = "B9407F30-F5F8-466E-AFF9-25556B57FE6D";


    [Theory]
    [InlineData(Uuid, null, null, Uuid, 1, 1, true)]
    [InlineData(Uuid, 1, null, Uuid, 1, 1, true)]
    [InlineData(Uuid, 1, 1, Uuid, 1, 1, true)]
    [InlineData(Uuid, 1, 1, Uuid, 1, 2, false)]
    [InlineData(Uuid, 1, 1, Uuid, 2, 1, false)]
    [InlineData(Uuid, 1, 1, "C9407F30-F5F8-466E-AFF9-25556B57FE6D", 1, 1, false)]
    public void IsBeaconInRegion(string regionUuid,
                                 int? regionMajor,
                                 int? regionMinor,
                                 string beaconUuid,
                                 ushort beaconMajor,
                                 ushort beaconMinor,
                                 bool expectedResult)
        => new BeaconRegion(
                "test",
                Guid.Parse(regionUuid),
                (ushort?)regionMajor,
                (ushort?)regionMinor
            )
            .IsBeaconInRegion(
                new Beacon(
                    Guid.Parse(beaconUuid),
                    beaconMajor,
                    beaconMinor,
                    Proximity.Far,
                    0,
                    0.0
                )
            )
            .Should()
            .Be(expectedResult);
}
