namespace Shiny.Beacons.Tests;


public class BeaconRegionTests
{
    static readonly Guid Uuid = new("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0");


    [Fact]
    public void UuidOnly_MatchesAnyMajorAndMinor()
    {
        var region = new BeaconRegion("test", Uuid);

        Assert.True(region.IsBeaconInRegion(Uuid, 0, 0));
        Assert.True(region.IsBeaconInRegion(Uuid, 65535, 65535));
        Assert.False(region.IsBeaconInRegion(Guid.NewGuid(), 0, 0));
    }


    [Fact]
    public void MajorOnly_MatchesAnyMinor()
    {
        var region = new BeaconRegion("test", Uuid, Major: 7);

        Assert.True(region.IsBeaconInRegion(Uuid, 7, 0));
        Assert.True(region.IsBeaconInRegion(Uuid, 7, 99));
        Assert.False(region.IsBeaconInRegion(Uuid, 8, 0));
    }


    [Fact]
    public void MajorAndMinor_MatchExactly()
    {
        var region = new BeaconRegion("test", Uuid, Major: 7, Minor: 9);

        Assert.True(region.IsBeaconInRegion(Uuid, 7, 9));
        Assert.False(region.IsBeaconInRegion(Uuid, 7, 10));
        Assert.False(region.IsBeaconInRegion(Uuid, 6, 9));
    }


    [Fact]
    public void MajorZero_IsLegal()
    {
        // the pre-revival region threw on major < 1, which made zero - a perfectly valid value -
        // impossible to monitor
        var region = new BeaconRegion("test", Uuid, Major: 0, Minor: 0);

        Assert.True(region.IsBeaconInRegion(Uuid, 0, 0));
        Assert.False(region.IsBeaconInRegion(Uuid, 1, 0));
    }


    [Fact]
    public void MinorWithoutMajor_Throws()
        => Assert.Throws<ArgumentException>(() => new BeaconRegion("test", Uuid, Minor: 1));


    [Fact]
    public void EmptyIdentifier_Throws()
        => Assert.Throws<ArgumentException>(() => new BeaconRegion("  ", Uuid));


    [Fact]
    public void EmptyUuid_Throws()
        => Assert.Throws<ArgumentException>(() => new BeaconRegion("test", Guid.Empty));
}
