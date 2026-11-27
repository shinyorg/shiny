using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shiny.Beacons;

[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(BeaconRegion))]
[JsonSerializable(typeof(List<BeaconRegion>))]
[JsonSerializable(typeof(BeaconRegion[]))]
#if APPLE && !MACOS
[JsonSerializable(typeof(BeaconMonitorState))]
[JsonSerializable(typeof(List<BeaconMonitorState>))]
[JsonSerializable(typeof(BeaconMonitorState[]))]
#endif
internal partial class ShinyBeaconsJsonContext : JsonSerializerContext;
