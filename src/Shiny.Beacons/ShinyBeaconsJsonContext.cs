using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shiny.Beacons;

[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(BeaconRegion))]
[JsonSerializable(typeof(List<BeaconRegion>))]
[JsonSerializable(typeof(BeaconRegion[]))]
internal partial class ShinyBeaconsJsonContext : JsonSerializerContext;
