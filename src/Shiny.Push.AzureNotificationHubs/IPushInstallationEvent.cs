using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;

namespace Shiny;

/// <summary>
/// Allows modification of Azure Notification Hub installation before it is sent to the server
/// </summary>
public interface IPushInstallationEvent
{
    /// <summary>
    /// Called before the installation is sent to Azure Notification Hubs, allowing modification of tags and templates
    /// </summary>
    /// <param name="installation">The installation to modify</param>
    Task OnBeforeSend(Installation installation);
}