namespace Shiny.Tests.Locations;


public class DistanceTests
{
    [Fact]
    public void MetersToKm() =>
        Distance
            .FromMeters(2000)
            .TotalKilometers
            .Should()
            .Be(2);


    [Fact]
    public void Int32MilesToKm() =>
        Distance
            .FromMiles(2)
            .TotalKilometers
            .Should()
            .Be(3.21868);


    [Fact]
    public void DoubleMilesToKm() =>
        Distance
            .FromMiles(2.0)
            .TotalKilometers
            .Should()
            .Be(3.21868);


    [Fact]
    public void KmToMiles() =>
        Distance
            .FromKilometers(2)
            .TotalMiles
            .Should()
            .Be(1.242742);


    [Fact]
    public void EqualsDistance() =>
        Distance
            .FromKilometers(1)
            .Equals(Distance.FromMeters(1000))
            .Should()
            .Be(true);


    [Fact]
    public void EqualsNull() =>
        Distance
            .FromKilometers(1)
            .Equals(null)
            .Should()
            .Be(false);


    [Fact]
    public void ToStringReturnsKilometers() =>
        Distance
            .FromKilometers(1)
            .ToString()
            .Should()
            .Be("[Distance: 1 km]");
}
