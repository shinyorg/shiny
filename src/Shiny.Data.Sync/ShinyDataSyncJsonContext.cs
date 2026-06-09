using System.Text.Json.Serialization;

namespace Shiny.Data.Sync;


[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(SyncOperation))]
[JsonSerializable(typeof(SyncCursor))]
[JsonSerializable(typeof(SyncTombstoneCursor))]
public partial class ShinyDataSyncJsonContext : JsonSerializerContext;
