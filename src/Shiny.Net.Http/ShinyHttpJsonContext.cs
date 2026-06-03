using System.Text.Json.Serialization;

namespace Shiny.Net.Http;

#if APPLE
[JsonSerializable(typeof(AppleHttpTransferRequest))]
#endif
[JsonSerializable(typeof(HttpTransfer))]
public partial class ShinyHttpJsonContext : JsonSerializerContext;
