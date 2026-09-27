using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shiny.Locations;

[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(GeofenceRegion))]
[JsonSerializable(typeof(List<GeofenceRegion>))]
[JsonSerializable(typeof(GeofenceRegion[]))]
[JsonSerializable(typeof(GeofenceDwellEntry))]
[JsonSerializable(typeof(List<GeofenceDwellEntry>))]
[JsonSerializable(typeof(GeofenceDwellEntry[]))]
internal partial class ShinyGeofencingJsonContext : JsonSerializerContext;
