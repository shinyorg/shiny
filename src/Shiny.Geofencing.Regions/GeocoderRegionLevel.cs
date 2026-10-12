namespace Shiny.Locations;


/// <summary>
/// Which part of a reverse-geocoded address a geocoder region set treats as the region.
/// </summary>
public enum GeocoderRegionLevel
{
    /// <summary>The country - id is the ISO country code (e.g. <c>US</c>).</summary>
    Country,

    /// <summary>The state, province or equivalent - id is <c>{CountryCode}-{AdministrativeArea}</c>.</summary>
    AdministrativeArea,

    /// <summary>The county or equivalent - id is <c>{CountryCode}-{AdministrativeArea}-{SubAdministrativeArea}</c>.</summary>
    SubAdministrativeArea,

    /// <summary>The city or town - id is <c>{CountryCode}-{AdministrativeArea}-{Locality}</c>.</summary>
    Locality,

    /// <summary>The neighbourhood or district - id is <c>{CountryCode}-{AdministrativeArea}-{Locality}-{SubLocality}</c>.</summary>
    SubLocality,

    /// <summary>The postal or zip code - id is <c>{CountryCode}-{PostalCode}</c>.</summary>
    PostalCode
}
