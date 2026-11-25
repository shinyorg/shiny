using System.Text.Json.Serialization;

namespace Shiny.Locations;

// the persisted listener request, read back at startup to restart it
[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(GpsRequest))]
internal partial class ShinyGpsLinuxJsonContext : JsonSerializerContext;
