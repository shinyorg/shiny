using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shiny.Data.Sync;


[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(SyncOperation))]
[JsonSerializable(typeof(List<SyncOperation>))]
[JsonSerializable(typeof(SyncOperation[]))]
[JsonSerializable(typeof(SyncCursor))]
[JsonSerializable(typeof(List<SyncCursor>))]
[JsonSerializable(typeof(SyncCursor[]))]
[JsonSerializable(typeof(SyncTombstoneCursor))]
[JsonSerializable(typeof(List<SyncTombstoneCursor>))]
[JsonSerializable(typeof(SyncTombstoneCursor[]))]
public partial class ShinyDataSyncJsonContext : JsonSerializerContext;
