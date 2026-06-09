using System.Text.Json.Serialization;

namespace Shiny.Data.Sync.Tests;


public record Item(string Identifier, string Name = "", bool IsArchived = false) : ISyncEntity;
public record SampleEntity(string Identifier, string Name) : ISyncEntity;
public record OtherEntity(string Identifier) : ISyncEntity;
public record NotRegistered(string Identifier) : ISyncEntity;


/// <summary>
/// [ShinyJsonContext] triggers a source-generated <c>[ModuleInitializer]</c> that registers
/// this context with <see cref="Shiny.Json.Default"/> at assembly load time, so every test
/// entity is reachable for AOT-safe (de)serialization without per-test plumbing.
/// </summary>
[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(Item))]
[JsonSerializable(typeof(SampleEntity))]
[JsonSerializable(typeof(OtherEntity))]
[JsonSerializable(typeof(NotRegistered))]
public partial class TestJsonContext : JsonSerializerContext;
