using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public class NotificationManagerImpl : INotificationManager
    {
        //     // create the notification
        //     var notification = new UILocalNotification();

        //     // set the fire date (the date time in which it will fire)
        //     notification.FireDate = NSDate.Now.AddSeconds(10); //DateTime.Now.AddSeconds(10));
        //notification.TimeZone = NSTimeZone.DefaultTimeZone;
        //// configure the alert stuff
        //notification.AlertTitle = "Alert Title";
        //notification.AlertAction = "Alert Action";
        //notification.AlertBody = "Alert Body: 10 sec alert fired!";

        //notification.UserInfo = NSDictionary.FromObjectAndKey(new NSString("UserInfo for notification"), new NSString("Notification"));

        //// modify the badge - has no effect on the Watch
        ////notification.ApplicationIconBadgeNumber = 1;

        //// set the sound to be the default sound
        ////notification.SoundName = UILocalNotification.DefaultSoundName;

        //// schedule it
        //UIApplication.SharedApplication.ScheduleLocalNotification(notification);
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
