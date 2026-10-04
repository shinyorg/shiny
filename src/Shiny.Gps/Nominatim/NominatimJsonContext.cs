using System.Text.Json.Serialization;

namespace Shiny.Locations;


[JsonSerializable(typeof(NominatimResponse))]
internal partial class NominatimJsonContext : JsonSerializerContext;


// https://nominatim.org/release-docs/latest/api/Output/ - format=jsonv2. A lookup that matched nothing comes back
// as HTTP 200 with only "error" set.
internal record NominatimResponse(
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("lat")] string? Latitude,
    [property: JsonPropertyName("lon")] string? Longitude,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("address")] NominatimAddress? Address
);


internal record NominatimAddress(
    [property: JsonPropertyName("house_number")] string? HouseNumber,
    [property: JsonPropertyName("road")] string? Road,
    [property: JsonPropertyName("pedestrian")] string? Pedestrian,
    [property: JsonPropertyName("neighbourhood")] string? Neighbourhood,
    [property: JsonPropertyName("suburb")] string? Suburb,
    [property: JsonPropertyName("quarter")] string? Quarter,
    [property: JsonPropertyName("city_district")] string? CityDistrict,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("town")] string? Town,
    [property: JsonPropertyName("village")] string? Village,
    [property: JsonPropertyName("hamlet")] string? Hamlet,
    [property: JsonPropertyName("municipality")] string? Municipality,
    [property: JsonPropertyName("county")] string? County,
    [property: JsonPropertyName("state_district")] string? StateDistrict,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("province")] string? Province,
    [property: JsonPropertyName("region")] string? Region,
    [property: JsonPropertyName("postcode")] string? Postcode,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("country_code")] string? CountryCode
);
