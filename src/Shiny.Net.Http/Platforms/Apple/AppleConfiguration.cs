using System;
using Foundation;

namespace Shiny.Net.Http;


public record AppleConfiguration(
    string? SessionName = null,
    int HttpMaximumConnectionsPerHost = 2,
    NSUrlRequestCachePolicy? CachePolicy = null,
    bool AllowsCellularAccess = true,
    bool AllowsConstrainedNetworkAccess = true,
    bool AllowsExpensiveNetworkAccess = true,
    bool? HttpShouldUsePipelining = null,
    bool? ShouldUseExtendedBackgroundIdleMode = null,
    bool? WaitsForConnectivity = null
);