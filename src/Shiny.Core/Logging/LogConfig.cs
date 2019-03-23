using System;


namespace Shiny.Logging
{
    public class LogConfig
    {
        public LogConfig(ILogger log) => this.Log = log;


        public bool IsCrashEnabled { get; set; }
        public bool IsEventsEnabled { get; set; }
        public ILogger Log { get; }
    }
}
