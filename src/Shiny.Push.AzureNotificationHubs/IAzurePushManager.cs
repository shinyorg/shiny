using System;
using System.Threading.Tasks;


namespace Shiny.Push.AzureNotificationHubs
{
    public interface IAzurePushManager
    {
        Task UpdateRegistrationToken(string newToken);
    }
}
