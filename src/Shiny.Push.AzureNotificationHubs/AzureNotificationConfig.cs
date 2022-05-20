using System;

namespace Shiny;


public record AzureNotificationConfig(
    string ListenerConnectionString,
    string HubName,

    /// <summary>
    /// If you are receiving InstallationId not found - setting this timer higher can sometimes help.  Azure often takes a second a to propagate after token creation
    /// </summary>
    int AzureAuthenticationWaitTimeMs = 1000,

    /// <summary>
    /// This will set a specific time to expire your tokens with Azure.  NOTE: that every call to RequestAccess or tag setting, will bump the expiration forward
    /// </summary>
    TimeSpan? ExpirationTime = null
);