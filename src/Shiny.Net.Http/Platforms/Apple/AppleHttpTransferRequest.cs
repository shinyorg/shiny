using System.Collections.Generic;

namespace Shiny.Net.Http;


public record AppleHttpTransferRequest(
    string Identifier,
    string Uri,
    bool IsUpload,
    string LocalFilePath,
    bool UseMeteredConnection = true,
    TransferHttpContent? BodyContent = null,
    IDictionary<string, string>? Headers = null,
    bool AllowsConstrainedNetworkAccess = true,
    bool AllowsCellularAccess = true,
    bool? AssumesHttp3Capable = null
    //native.NetworkServiceType = NSUrlRequestNetworkServiceType.Background
    //native.RequiresDnsSecValidation
    //native.TimeoutInterval

)
: HttpTransferRequest(
    Identifier,
    Uri,
    IsUpload,
    LocalFilePath,
    UseMeteredConnection,
    BodyContent,
    Headers
);