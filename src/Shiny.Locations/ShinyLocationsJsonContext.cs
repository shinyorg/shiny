using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shiny.Locations;

[Shiny.ShinyJsonContext]
#if APPLE
[JsonSerializable(typeof(AppleGpsRequest))]
[JsonSerializable(typeof(List<AppleGpsRequest>))]
[JsonSerializable(typeof(AppleGpsRequest[]))]
#elif ANDROID
[JsonSerializable(typeof(AndroidGpsRequest))]
[JsonSerializable(typeof(List<AndroidGpsRequest>))]
[JsonSerializable(typeof(AndroidGpsRequest[]))]
#endif
[JsonSerializable(typeof(GeofenceRegion))]
[JsonSerializable(typeof(List<GeofenceRegion>))]
[JsonSerializable(typeof(GeofenceRegion[]))]
internal partial class ShinyLocationsJsonContext : JsonSerializerContext;
