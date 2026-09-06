using System.Text.Json.Serialization;

namespace Shiny.LiveActivities;


/// <summary>
/// Source-generated serialization for the small amount of state the renderer persists, so it works under
/// trimming and Native AOT without a reflection fallback.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class LiveActivityRendererJsonContext : JsonSerializerContext;
