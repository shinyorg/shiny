using System;
using System.Threading.Tasks;
using Shiny.Push;


namespace Shiny
{
    public static class CrossPushNotifications
    {
        static IPushNotificationManager Current { get; } = ShinyHost.Resolve<IPushNotificationManager>();

        public static Task<PushAccessState> RequestAccess() => Current.RequestAccess();
    }
}
