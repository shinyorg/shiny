using System;
using System.Collections.Generic;


namespace Shiny.Logging
{
    public struct LogEvent
    {
        public LogEvent(string eventName, string description, params (string, string)[] parameters)
        {
            this.EventName = eventName;
            this.Description = description;
            this.Parameters = parameters.ToDictionary();
        }


        public string EventName { get; }
        public string Description { get; }
        public IDictionary<string, string> Parameters { get; }
    }
}
