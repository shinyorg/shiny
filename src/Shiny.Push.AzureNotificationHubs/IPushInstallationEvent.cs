using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;

namespace Shiny;

/// <summary>
/// Hook invoked before an Azure Notification Hubs installation is sent to the server, allowing tags and templates to be customized.
/// </summary>
public interface IPushInstallationEvent
{
    /// <summary>
    /// Called just before the installation is submitted to Azure Notification Hubs. Mutate the supplied installation to add tags or templates.
    /// </summary>
    /// <param name="installation">The installation about to be sent; modify in place.</param>
    Task OnBeforeSend(Installation installation);
}
