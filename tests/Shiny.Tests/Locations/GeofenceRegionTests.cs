using Shiny.Locations;

namespace Shiny.Tests.Locations;


public class GeofenceRegionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("test")]
    public void GetHashCodeConsidersOnlyIdentifier(string id) =>
        new GeofenceRegion(id, new Position(0, 0), Distance.FromKilometers(0))
            .GetHashCode()
            .Should()
            .Be(new GeofenceRegion(id, null, null).GetHashCode());


    [Theory]
    [InlineData(null, null, true)]
    [InlineData("test", null, false)]
    [InlineData(null, "test", false)]
    [InlineData("test", "test", true)]
    public void EqualsComparesIdentifier(string id1, string id2, bool expected) =>
        new GeofenceRegion(id1, null, null)
            .Equals(new GeofenceRegion(id2, null, null))
            .Should()
            .Be(expected);


    [Fact]
    public void ToStringReturnsIdentifier() =>
        new GeofenceRegion("test", null, null)
            .ToString()
            .Should()
            .Be("[Identifier: test]");
}
