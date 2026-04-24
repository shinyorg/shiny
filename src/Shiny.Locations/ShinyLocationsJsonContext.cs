using System.Text.Json.Serialization;

namespace Shiny.Locations;

[JsonSerializable(typeof(GeofenceRegion))]
internal partial class ShinyLocationsJsonContext : JsonSerializerContext;
