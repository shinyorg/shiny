using System.Collections.Generic;

namespace Shiny.Net.Http;


public record HttpTransferRequest(
    string Uri,
    bool IsUpload,
    string LocalFilePath,
    bool UseMeteredConnection = true,
    string? PostData = null,
    string? HttpMethod = null,
    IDictionary<string, string>? Headers = null
);