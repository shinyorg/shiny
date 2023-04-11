using System;
using Foundation;

namespace Shiny.Net.Http;


public record AppleConfiguration(
    int? HttpMaximumConnectionsPerHost = null,
    NSUrlRequestCachePolicy? CachePolicy = null,
    bool AllowsCellularAccess = true,
    bool AllowsConstrainedNetworkAccess = true,
    bool AllowsExpensiveNetworkAccess = true,
    bool? HttpShouldUsePipelining = null
);