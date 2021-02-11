//using System;
//using System.Linq;
//using Shiny.Logging;


//namespace Shiny.Notifications
//{
//    public class NotificationLogger : ILogger
//    {
//        public async void Write(Exception exception, params (string Key, string Value)[] parameters)
//        {
//            var manager = ShinyHost.Resolve<INotificationManager>();
//            var notification = new Notification
//            {
//                Title = "ERROR",
//                Message = exception.ToString()
//            };
//            parameters?.ToList().ForEach(x => notification.Payload.Add(x.Key, x.Value));
//            notification.Payload.Add("ERROR", exception.ToString());

//            await manager.Send(notification);
//        }


//        public void Write(string eventName, string description, params (string Key, string Value)[] parameters) { }
//    }
//}
