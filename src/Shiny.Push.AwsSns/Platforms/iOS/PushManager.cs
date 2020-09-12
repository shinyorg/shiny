using System;
using Shiny.Notifications;
using Shiny.Settings;


namespace Shiny.Push.AwsSns
{
    public class PushManager : Shiny.Push.PushManager
    {
        public PushManager(ISettings settings, IServiceProvider services, iOSNotificationDelegate nativeDelegate) : base(settings, services, nativeDelegate)
        {
        }
    }
}
