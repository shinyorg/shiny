using System;


namespace Shiny.Push.AzureNotificationHubs
{
    public class AzureNotificationConfig
    {
        public AzureNotificationConfig(string listenerConnectionString, string hubName)
        {
            this.ListenerConnectionString = listenerConnectionString;
            this.HubName = hubName;
        }


        public string ListenerConnectionString { get; }
        public string HubName { get; }

        /// <summary>
        /// If you are receiving InstallationId not found - setting this timer higher can sometimes help.  Azure often takes a second a to propagate after token creation
        /// </summary>
        public int AzureAuthenticationWaitTimeMs { get; set; } = 1000;
    }
}
