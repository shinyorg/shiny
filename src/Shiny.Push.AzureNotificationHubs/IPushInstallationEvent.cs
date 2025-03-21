using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;

namespace Shiny;

public interface IPushInstallationEvent
{
    Task OnBeforeSend(Installation installation);
}