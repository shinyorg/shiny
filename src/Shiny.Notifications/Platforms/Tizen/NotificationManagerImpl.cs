using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tizen.Applications.Notifications;
using NativeNot = Tizen.Applications.Notifications.Notification;


namespace Shiny.Notifications
{
    //https://developer.tizen.org/development/guides/.net-application/notifications-and-content-sharing/notifications
    public class NotificationManagerImpl : INotificationManager
    {
        public Task Cancel(int id)
        {
            NotificationManager.Delete(new NativeNot
            {

            });
            return Task.CompletedTask;
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
            NotificationManager.Post(new NativeNot
            {

            });
            return Task.CompletedTask;
        }
    }
}
