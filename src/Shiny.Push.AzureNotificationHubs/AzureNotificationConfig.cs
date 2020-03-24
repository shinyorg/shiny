﻿using System;


namespace Shiny.Push.AzureNotifications
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
    }
}
