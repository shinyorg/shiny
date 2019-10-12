using System;


namespace Shiny.Notifications
{
    public static class CrossNotifications
    {
        public static INotificationManager Current => ShinyHost.Resolve<INotificationManager>();


        //public static void RegisterDelegate<>
    }
}
