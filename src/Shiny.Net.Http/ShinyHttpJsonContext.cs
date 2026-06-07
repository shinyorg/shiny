using System.Text.Json.Serialization;

namespace Shiny.Net.Http;

[Shiny.ShinyJsonContext]
#if APPLE
[JsonSerializable(typeof(AppleHttpTransferRequest))]
#endif
[JsonSerializable(typeof(HttpTransfer))]
public partial class ShinyHttpJsonContext : JsonSerializerContext;
