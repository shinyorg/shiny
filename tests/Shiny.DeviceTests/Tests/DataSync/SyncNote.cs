using System.Text.Json.Serialization;
using Shiny.Data.Sync;

namespace Shiny.DeviceTests.Tests;


public record SyncNote(string Identifier, string Title, int Revision) : ISyncEntity;


// Shiny.Data.Sync serializes through Shiny.Json.Default - [ShinyJsonContext] wires this in with a module initializer
[ShinyJsonContext]
[JsonSerializable(typeof(SyncNote))]
public partial class DataSyncTestJsonContext : JsonSerializerContext;


// mirror of the runner's DataSyncFixture write log entry
public record SyncWrite(string EntityId, string Method, string Verb, string? Payload, int Status, int Attempt);


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<SyncWrite>))]
[JsonSerializable(typeof(List<string>))]
public partial class DataSyncFixtureJsonContext : JsonSerializerContext;
