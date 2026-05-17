using System;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;

namespace Shiny;


/// <summary>
/// Configuration for the Shiny Azure Notification Hubs push provider.
/// </summary>
/// <param name="ListenerConnectionString">The DefaultListenSharedAccessSignature connection string for the hub.</param>
/// <param name="HubName">The Azure Notification Hub name.</param>
/// <param name="AzureAuthenticationWaitTimeMs">Delay (ms) after token creation to allow Azure to propagate the install before registering. Increase if you receive InstallationId not found errors.</param>
/// <param name="ExpirationTime">Optional expiration for the Azure registration; every RequestAccess or tag change extends the expiration forward.</param>
/// <param name="BeforeSendInstallation">Optional callback invoked just before the installation is sent to ANH, for customizing tags and templates.</param>
public record AzureNotificationConfig(
    string ListenerConnectionString,
    string HubName,
    int AzureAuthenticationWaitTimeMs = 1000,
    TimeSpan? ExpirationTime = null,
    Func<Installation, Task>? BeforeSendInstallation = null
);
