namespace Shiny.Locations;


/// <summary>
/// A human-readable description of a location returned by <see cref="IGeocoder"/>. Any component the platform
/// geocoder could not resolve is null.
/// </summary>
/// <param name="Position">The position the placemark describes.</param>
/// <param name="Name">The name of the place (a landmark or business name, or the street address).</param>
/// <param name="SubThoroughfare">The street number.</param>
/// <param name="Thoroughfare">The street name.</param>
/// <param name="SubLocality">The neighbourhood or district.</param>
/// <param name="Locality">The city or town.</param>
/// <param name="SubAdministrativeArea">The county or equivalent.</param>
/// <param name="AdministrativeArea">The state, province or equivalent.</param>
/// <param name="PostalCode">The postal or zip code.</param>
/// <param name="CountryCode">The ISO 3166-1 alpha-2 country code.</param>
/// <param name="CountryName">The localized country name.</param>
/// <param name="FormattedAddress">The full address as a single line, formatted by the platform.</param>
public record Placemark(
    Position Position,
    string? Name,
    string? SubThoroughfare,
    string? Thoroughfare,
    string? SubLocality,
    string? Locality,
    string? SubAdministrativeArea,
    string? AdministrativeArea,
    string? PostalCode,
    string? CountryCode,
    string? CountryName,
    string? FormattedAddress
);
