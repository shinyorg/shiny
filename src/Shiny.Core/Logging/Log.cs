using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace Shiny.Logging
{
    public static class Log
    {
        static readonly Subject<(Exception Exception, (string Key, string Value)[] Parameters)> errorSubj = new Subject<(Exception Exception, (string Key, string Value)[] Parameters)>();
        public static IObservable<(Exception Exception, (string Key, string Value)[] Parameters)> WhenExceptionLogged() => errorSubj;


        static readonly Subject<(string EventName, string Description, (string Key, string Value)[] Parameters)> eventSubj = new Subject<(string EventName, string Description, (string Key, string Value)[] Parameters)>();
        public static IObservable<(string EventName, string Description, (string Key, string Value)[] Parameters)> WhenEventLogged() => eventSubj;
#if DEBUG
        static Log() => UseDebug();
#endif



        public static void Write(Exception exception, params (string Key, string Value)[] parameters)
        {
            errorSubj.OnNext((exception, parameters));
            foreach (var log in Loggers)
            {
                if (log.IsCrashEnabled)
                    DoLog(log.Log, exception, parameters);
            }
        }


        public static void Write(string eventName, string description, params (string Key, string Value)[] parameters)
        {
            eventSubj.OnNext((eventName, description, parameters));
            foreach (var log in Loggers)
                if (log.IsEventsEnabled)
                    DoLog(log.Log, eventName, description, parameters);
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


        static void DoLog(ILogger logger, Exception ex, (string Key, string Value)[] parameters)
        {
            try
            {
                logger.Write(ex, parameters);
            }
            catch (Exception loggerEx)
            {
                Console.WriteLine($"The logger '{logger.GetType().FullName}' failed with {loggerEx}");
            }
        }


        static void DoLog(ILogger logger, string eventName, string description, (string Key, string Value)[] parameters)
        {
            try
            {
                logger.Write(eventName, description, parameters);
            }
            catch (Exception loggerEx)
            {
                Console.WriteLine($"The logger '{logger.GetType().FullName}' failed with {loggerEx}");
            }
        }
    }
}
