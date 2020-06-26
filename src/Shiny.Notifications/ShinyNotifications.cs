//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Shiny.Notifications;

//namespace Shiny
//{
//    public static class ShinyNotifications
//    {
//        static INotificationManager Current { get; } = ShinyHost.Resolve<INotificationManager>();

//        public static Task Cancel(int id) => Current.Cancel(id);
//        public static Task Clear() => Current.Clear();
//        public static int Badge
//        {
//            get => Current.Badge;
//            set => Current.Badge = value;
//        }
//        public static Task<IEnumerable<Notification>> GetPending() => Current.GetPending();
//        public static Task<AccessState> RequestAccess() => Current.RequestAccess();
//        public static Task Send(Notification notification) => Current.Send(notification);
//    }
//}
