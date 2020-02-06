using System;
using Shiny.Settings;


namespace Shiny.Integrations.FirebaseNotifications
{
    public class PushManager : Shiny.Push.PushManager
    {
        public PushManager(ISettings settings) : base(settings)
        {
        }
    }
}
