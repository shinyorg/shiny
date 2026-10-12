using Shiny.DocumentDb;

namespace Shiny.Locations.NorthAmerica;

/// <summary>
/// A first-level administrative region — a US state or a Canadian province — with a simplified polygon
/// boundary suitable for point-in-region ("which state is this coordinate in?") queries.
/// </summary>
/// <remarks>
/// Boundaries are intentionally low-resolution (a handful of vertices each) so the whole dataset stays
/// tiny and embeddable. They are accurate enough for coarse containment tests, not for cartography.
/// </remarks>
public class GeoRegion
{
    /// <summary>
    /// Stable document id: <c>{CountryCode}-{Abbreviation}</c> (e.g. <c>US-CA</c>, <c>CA-ON</c>).
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>ISO 3166-1 alpha-2 country code — <c>US</c> or <c>CA</c>.</summary>
    public string CountryCode { get; set; } = null!;

    /// <summary>Full region name (e.g. <c>California</c>, <c>Ontario</c>).</summary>
    public string Name { get; set; } = null!;

    /// <summary>Postal/ISO abbreviation (e.g. <c>CA</c>, <c>ON</c>).</summary>
    public string Abbreviation { get; set; } = null!;

    /// <summary>Population from the most recent census baked into the dataset.</summary>
    public long Population { get; set; }

    /// <summary>
    /// Simplified boundary polygon (WGS84). Typed as the <see cref="Geometry"/> base so the polymorphic
    /// GeoJSON converter round-trips it; every region in the embedded dataset is a <see cref="GeoPolygon"/>.
    /// </summary>
    public Geometry Boundary { get; set; } = null!;
}
