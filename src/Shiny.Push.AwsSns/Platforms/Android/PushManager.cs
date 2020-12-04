using System;
using Shiny.Notifications;
using Shiny.Settings;


namespace Shiny.Push.AwsSns
{
    public class PushManager : Shiny.Push.PushManager
    {
        public PushManager(IAndroidContext context, INotificationManager notifications, ISettings settings, IMessageBus bus) : base(context, notifications, settings, bus)
        {
        }
    }
}
