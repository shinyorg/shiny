using System;
using System.Collections.Generic;

namespace Shiny.Logging
{
    public static class Log
    {
#if DEBUG
        static Log() => UseDebug();
#endif

        public static void Write(Exception exception, params (string Key, string Value)[] parameters)
        {
            foreach (var log in Loggers)
                if (log.IsEventsEnabled)
                    log.Log.Write(exception, parameters);
        }


        public static void Write(string eventName, string description, params (string Key, string Value)[] parameters)
        {
            foreach (var log in Loggers)
                if (log.IsEventsEnabled)
                    log.Log.Write(eventName, description, parameters);
        }


        public static IList<LogConfig> Loggers { get; } = new List<LogConfig>();


        public static void AddLogger(ILogger log, bool crashesEnabled = true, bool eventsEnabled = true)
            => Loggers.Add(new LogConfig(log)
            {
                IsCrashEnabled = crashesEnabled,
                IsEventsEnabled = eventsEnabled
            });


        public static void UseDebug(bool crashesEnabled = true, bool eventsEnabled = true)
            => AddLogger(new DebugLogger(), crashesEnabled, eventsEnabled);

        public static void UseConsole(bool crashesEnabled = true, bool eventsEnabled = true)
            => AddLogger(new ConsoleLogger(), crashesEnabled, eventsEnabled);
    }
}
