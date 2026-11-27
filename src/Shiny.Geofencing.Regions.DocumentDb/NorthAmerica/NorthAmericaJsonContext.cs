using System.Text.Json.Serialization;
using Shiny.DocumentDb;

namespace Shiny.Locations.NorthAmerica;


/// <summary>
/// Source-generated metadata for every document this package writes. <see cref="NorthAmericaExtensions.MapNorthAmericaData"/>
/// puts it at the front of the store's resolver chain, so the store never falls back to reflection for them - under
/// trimming / NativeAOT that fallback sees properties whose getters were removed (the seed marker's <c>Id</c> is only
/// ever set in code) and fails at runtime.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeoRegion))]
[JsonSerializable(typeof(GeoCity))]
[JsonSerializable(typeof(DocumentSeedMarker))]
partial class NorthAmericaJsonContext : JsonSerializerContext;
