using Shiny.DocumentDb;

namespace Shiny.Locations.NorthAmerica;

/// <summary>
/// A populated city/municipality represented as a single WGS84 point (its civic centre), with its
/// region and population.
/// </summary>
public class GeoCity
{
    /// <summary>
    /// Stable document id: <c>{CountryCode}-{RegionCode}-{slug(Name)}</c> (e.g. <c>US-CA-los-angeles</c>).
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>ISO 3166-1 alpha-2 country code — <c>US</c> or <c>CA</c>.</summary>
    public string CountryCode { get; set; } = null!;

    /// <summary>
    /// The abbreviation of the containing <see cref="GeoRegion"/> (state/province, e.g. <c>CA</c>, <c>ON</c>).
    /// </summary>
    public string RegionCode { get; set; } = null!;

    /// <summary>City name (e.g. <c>Los Angeles</c>).</summary>
    public string Name { get; set; } = null!;

    /// <summary>Population from the most recent census baked into the dataset.</summary>
    public long Population { get; set; }

    /// <summary>The city's point location (WGS84).</summary>
    public GeoPoint Location { get; set; }
}
