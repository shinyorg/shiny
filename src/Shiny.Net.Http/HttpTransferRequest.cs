using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace Shiny.Net.Http;


public record HttpTransferRequest(
    string Uri,
    bool IsUpload,
    FileInfo LocalFile,
    bool UseMeteredConnection = true,
    string? PostData = null,
    HttpMethod? HttpMethod = null,
    IDictionary<string, string>? Headers = null
);