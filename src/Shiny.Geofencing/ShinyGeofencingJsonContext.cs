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
#if APPLE
[JsonSerializable(typeof(GeofenceMonitorState))]
[JsonSerializable(typeof(List<GeofenceMonitorState>))]
[JsonSerializable(typeof(GeofenceMonitorState[]))]
#endif
internal partial class ShinyGeofencingJsonContext : JsonSerializerContext;
