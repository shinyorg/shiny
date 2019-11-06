using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Notifications;

namespace Shiny
{
    public static class CrossNotifications
    {
        static INotificationManager Current { get; } = ShinyHost.Resolve<INotificationManager>();

        public static Task Cancel(int id) => Current.Cancel(id);
        public static Task Clear() => Current.Clear();
        public static Task<int> GetBadge() => Current.GetBadge();
        public static Task<IEnumerable<Notification>> GetPending() => Current.GetPending();
        public static Task<AccessState> RequestAccess() => Current.RequestAccess();
        public static Task Send(Notification notification) => Current.Send(notification);
        public static Task SetBadge(int value) => Current.SetBadge(value);
    }
}
