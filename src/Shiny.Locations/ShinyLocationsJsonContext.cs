using System.Text.Json.Serialization;

namespace Shiny.Locations;

[Shiny.ShinyJsonContext]
#if APPLE
[JsonSerializable(typeof(AppleGpsRequest))]
#elif ANDROID
[JsonSerializable(typeof(AndroidGpsRequest))]
#endif
[JsonSerializable(typeof(GeofenceRegion))]
internal partial class ShinyLocationsJsonContext : JsonSerializerContext;
