using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shiny.Net.Http;

[Shiny.ShinyJsonContext]
#if APPLE
[JsonSerializable(typeof(AppleHttpTransferRequest))]
[JsonSerializable(typeof(List<AppleHttpTransferRequest>))]
[JsonSerializable(typeof(AppleHttpTransferRequest[]))]
#endif
[JsonSerializable(typeof(HttpTransfer))]
[JsonSerializable(typeof(List<HttpTransfer>))]
[JsonSerializable(typeof(HttpTransfer[]))]
public partial class ShinyHttpJsonContext : JsonSerializerContext;
