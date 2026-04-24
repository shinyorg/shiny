using System.Text.Json.Serialization;

namespace Shiny.Net.Http;

[JsonSerializable(typeof(HttpTransfer))]
public partial class ShinyHttpJsonContext : JsonSerializerContext;
