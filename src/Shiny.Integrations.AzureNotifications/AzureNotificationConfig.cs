using System;


namespace Shiny.Push.AzureNotifications
{
    public class AzureNotificationConfig
    {
        public AzureNotificationConfig(string hubName, string listenerConnectionString)
        {
            this.HubName = hubName;
            this.ListenerConnectionString = listenerConnectionString;
        }


        public string HubName { get; }
        public string ListenerConnectionString { get; }
    }
}
