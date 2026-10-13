#if APPLE || ANDROID
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shiny.Locations;

[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(MotionActivityReading))]
[JsonSerializable(typeof(List<MotionActivityReading>))]
[JsonSerializable(typeof(MotionActivityReading[]))]
#if APPLE
[JsonSerializable(typeof(AppleGpsRequest))]
[JsonSerializable(typeof(List<AppleGpsRequest>))]
[JsonSerializable(typeof(AppleGpsRequest[]))]
#elif ANDROID
[JsonSerializable(typeof(AndroidGpsRequest))]
[JsonSerializable(typeof(List<AndroidGpsRequest>))]
[JsonSerializable(typeof(AndroidGpsRequest[]))]
#endif
internal partial class ShinyGpsJsonContext : JsonSerializerContext;
#endif
