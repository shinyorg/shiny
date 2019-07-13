using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public class NotificationManagerImpl : INotificationManager
    {
        public Task Cancel(int id)
        {
            throw new NotImplementedException();
        }

        public Task Clear()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Notification>> GetPending()
        {
            throw new NotImplementedException();
        }

        public Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }

        public Task Send(Notification notification)
        {
            throw new NotImplementedException();
        }
    }
}
