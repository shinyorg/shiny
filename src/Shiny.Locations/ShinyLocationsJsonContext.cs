using System.Text.Json.Serialization;

namespace Shiny.Locations;

#if APPLE
[JsonSerializable(typeof(AppleGpsRequest))]
#elif ANDROID
[JsonSerializable(typeof(AndroidGpsRequest))]
#endif
[JsonSerializable(typeof(GeofenceRegion))]
internal partial class ShinyLocationsJsonContext : JsonSerializerContext;
