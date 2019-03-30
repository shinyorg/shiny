using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Acr.Notifications
{
    public class NotificationManager : INotificationManager
    {
        public Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Notification>> GetPendingNotifications()
        {
            throw new NotImplementedException();
        }

        public Task CancelAll()
        {
            throw new NotImplementedException();
        }

        public Task Cancel(int notificationId)
        {
            throw new NotImplementedException();
        }

        public Task Send(Notification notification)
        {
            throw new NotImplementedException();
        }
    }
}
