using System;
using Shiny;
using Shiny.Locations;
using Xunit;

namespace Shiny.Locations.Tests;


public class DistanceTests
{
    // -------- Unit constructors + accessors --------

    [Fact]
    public void FromKilometers_RoundTripsTotalKilometers()
        => Assert.Equal(5, Distance.FromKilometers(5).TotalKilometers);


    [Fact]
    public void FromMeters_RoundTripsTotalMeters()
        => Assert.Equal(500, Distance.FromMeters(500).TotalMeters, 6);


    [Fact]
    public void FromMiles_RoundTripsTotalMiles()
        => Assert.Equal(10, Distance.FromMiles(10).TotalMiles, 4);


    [Fact]
    public void FromMiles_Int_MatchesDoubleOverload()
        => Assert.Equal(Distance.FromMiles(3.0).TotalKilometers, Distance.FromMiles(3).TotalKilometers);


    [Fact]
    public void Conversion_OneKilometer_EqualsOneThousandMeters()
        => Assert.Equal(1000, Distance.FromKilometers(1).TotalMeters);


    [Fact]
    public void Conversion_OneMile_MatchesPublicConstant()
        => Assert.Equal(Distance.MILES_TO_KM, Distance.FromMiles(1.0).TotalKilometers, 6);


    // -------- Comparison operators --------

    [Fact]
    public void Operators_OrderByKilometers()
    {
        var a = Distance.FromKilometers(1);
        var b = Distance.FromKilometers(2);

        Assert.True(b > a);
        Assert.True(a < b);
        Assert.True(b >= a);
        Assert.True(a <= b);
        Assert.True(Distance.FromKilometers(1) <= Distance.FromKilometers(1));
        Assert.True(Distance.FromKilometers(1) >= Distance.FromKilometers(1));
    }


    // -------- Great-circle distance --------

    static readonly Position Toronto = new(43.6532, -79.3832);
    static readonly Position Montreal = new(45.5017, -73.5673);
    static readonly Position London = new(51.5074, -0.1278);
    static readonly Position Sydney = new(-33.8688, 151.2093);


    [Fact]
    public void Between_SamePoint_IsZero()
        => Assert.Equal(0, Distance.Between(Toronto, Toronto).TotalKilometers, 6);


    [Fact]
    public void Between_TorontoMontreal_IsApprox504km()
    {
        var km = Distance.Between(Toronto, Montreal).TotalKilometers;
        Assert.InRange(km, 500, 510);
    }


    [Fact]
    public void Between_IsSymmetric()
    {
        var ab = Distance.Between(Toronto, London).TotalKilometers;
        var ba = Distance.Between(London, Toronto).TotalKilometers;
        Assert.Equal(ab, ba, 6);
    }


    [Fact]
    public void Between_Antipodal_IsApproxHalfEarthCircumference()
    {
        // Sydney → London is ~17000 km — well within great-circle expectations
        var km = Distance.Between(Sydney, London).TotalKilometers;
        Assert.InRange(km, 16500, 17500);
    }


    [Fact]
    public void Between_OneMeterApart_IsRoughlyOneMeter()
    {
        // ~111,000 m per degree of latitude — 0.00001° lat ≈ 1.1m
        var meters = Distance.Between(
            new Position(0, 0),
            new Position(0.00001, 0)
        ).TotalMeters;
        Assert.InRange(meters, 0.9, 1.3);
    }
}
