using System;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public class AndroidForegroundServiceProvider
    {
        public async Task<IPersistentNotification> Start(Notification notification, Action service)
        {
            return null;
        }
    }
}
