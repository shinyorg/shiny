using System;
using Shiny.Notifications;

namespace Shiny
{
    public static class CrossNotifications
    {
        public static INotificationManager Current => ShinyHost.Resolve<INotificationManager>();
    }
}
