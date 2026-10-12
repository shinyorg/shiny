using System;
using System.Collections.Generic;
using System.Text;
using Shiny.DocumentDb;
using Shiny.Locations.NorthAmerica.Data;

namespace Shiny.Locations.NorthAmerica;

/// <summary>
/// The embedded reference dataset — US states, Canadian provinces, and US &amp; Canadian cities — projected
/// into <see cref="GeoRegion"/> and <see cref="GeoCity"/> documents. Materialized once and cached.
/// <para>
/// Use <see cref="NorthAmericaSeeder"/> to write these into a store, or read them directly for in-memory
/// lookups / custom seeding.
/// </para>
/// </summary>
public static class NorthAmericaData
{
    /// <summary>ISO country code for the United States.</summary>
    public const string UnitedStates = "US";

    /// <summary>ISO country code for Canada.</summary>
    public const string Canada = "CA";

    static readonly Lazy<IReadOnlyList<GeoRegion>> regions = new(BuildRegions);
    static readonly Lazy<IReadOnlyList<GeoCity>> cities = new(BuildCities);

    /// <summary>All US states and Canadian provinces (64 regions) with simplified boundaries.</summary>
    public static IReadOnlyList<GeoRegion> Regions => regions.Value;

    /// <summary>All US and Canadian cities as points.</summary>
    public static IReadOnlyList<GeoCity> Cities => cities.Value;

    static IReadOnlyList<GeoRegion> BuildRegions()
    {
        var list = new List<GeoRegion>();

        foreach (var s in UsStates.GetAll())
            list.Add(BuildRegion(UnitedStates, s.Name, s.Abbreviation, s.Population, s.Boundary));

        foreach (var p in CanadianProvinces.GetAll())
            list.Add(BuildRegion(Canada, p.Name, p.Abbreviation, p.Population, p.Boundary));

        return list;
    }

    static GeoRegion BuildRegion(string country, string name, string abbrev, long population, (double Lon, double Lat)[] boundary)
    {
        var ring = new GeoPoint[boundary.Length];
        for (var i = 0; i < boundary.Length; i++)
            ring[i] = new GeoPoint(boundary[i].Lat, boundary[i].Lon);

        return new GeoRegion
        {
            Id = $"{country}-{abbrev}",
            CountryCode = country,
            Name = name,
            Abbreviation = abbrev,
            Population = population,
            Boundary = new GeoPolygon(ring)
        };
    }

    static IReadOnlyList<GeoCity> BuildCities()
    {
        var list = new List<GeoCity>();

        foreach (var c in UsCities.GetAll())
            list.Add(BuildCity(UnitedStates, c.Name, c.StateAbbreviation, c.Population, c.Longitude, c.Latitude));

        foreach (var c in CanadianCities.GetAll())
            list.Add(BuildCity(Canada, c.Name, c.ProvinceAbbreviation, c.Population, c.Longitude, c.Latitude));

        return list;
    }

    static GeoCity BuildCity(string country, string name, string regionCode, long population, double lon, double lat)
        => new()
        {
            Id = $"{country}-{regionCode}-{Slug(name)}",
            CountryCode = country,
            RegionCode = regionCode,
            Name = name,
            Population = population,
            Location = new GeoPoint(lat, lon)
        };

    static string Slug(string value)
    {
        var sb = new StringBuilder(value.Length);
        var lastDash = false;
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastDash = false;
            }
            else if (!lastDash && sb.Length > 0)
            {
                sb.Append('-');
                lastDash = true;
            }
        }
        return sb.ToString().Trim('-');
    }
}
