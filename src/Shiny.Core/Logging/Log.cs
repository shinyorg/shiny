using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Logging
{
    public static class LoggingExtensions
    {
        public static void AddLogging<T>(this IServiceCollection services, bool crashesEnabled = true, bool eventsEnabled = true) where T : class, ILogger
        {
            services.AddSingleton<ILogger, T>();
            services.RegisterPostBuildAction(sp =>
            {
                var logger = sp.Resolve<ILogger>();
                Log.AddLogger(new ActionLogger(
                    (ex, parameters) => logger.Write(ex, parameters),
                    (ev, desc, parameters) => logger.Write(ev, desc, parameters)
                ), crashesEnabled, eventsEnabled);
            });
        }
    }


    public static class Log
    {
        public static void UseDebug(bool crashesEnabled = true, bool eventsEnabled = true)
            => AddLogger(new DebugLogger(), crashesEnabled, eventsEnabled);

        public static void UseConsole(bool crashesEnabled = true, bool eventsEnabled = true)
            => AddLogger(new ConsoleLogger(), crashesEnabled, eventsEnabled);

        public static void UseFile(bool crashesEnabled = true, bool eventsEnabled = true, string? logFileName = null)
            => AddLogger(new FileLogger(logFileName), crashesEnabled, eventsEnabled);


        static readonly Subject<LogError> errorSubj = new Subject<LogError>();
        public static IObservable<LogError> WhenExceptionLogged() => errorSubj;


        static readonly Subject<LogEvent> eventSubj = new Subject<LogEvent>();
        public static IObservable<LogEvent> WhenEventLogged() => eventSubj;


        public static IReadOnlyDictionary<string, string> Properties => new ReadOnlyDictionary<string, string>(properties);
        static IDictionary<string, string> properties = new Dictionary<string, string>();


        public static void SetProperty(string name, string value)
        {
            lock (properties)
                properties[name] = value;
        }


        public static void RemoveProperty(string name)
        {
            lock (properties)
                properties.Remove(name);
        }


        public static void ClearProperties()
        {
            lock (properties)
                properties.Clear();
        }


        public static void SafeExecute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }


        public static async Task SafeExecute(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }


        public static void Write(Exception exception, params (string Key, string Value)[] parameters)
        {
            var parms = Merge(parameters);
            errorSubj.OnNext(new LogError(exception, parms));
            foreach (var log in Loggers)
            {
                if (log.IsCrashEnabled)
                    DoLog(log.Log, exception, parms);
            }
        }


        public static void Write(string eventName, string description, params (string Key, string Value)[] parameters)
        {
            var parms = Merge(parameters);
            eventSubj.OnNext(new LogEvent(eventName, description, parms));
            foreach (var log in Loggers)
                if (log.IsEventsEnabled)
                    DoLog(log.Log, eventName, description, parms);
        }


        public static IList<LogConfig> Loggers { get; } = new List<LogConfig>();


        public static void AddLogger(ILogger log, bool crashesEnabled = true, bool eventsEnabled = true)
            => Loggers.Add(new LogConfig(log)
            {
                IsCrashEnabled = crashesEnabled,
                IsEventsEnabled = eventsEnabled
            });


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


        static (string Key, string Value)[] Merge(params (string Key, string Value)[] parameters)
        {
            var dict = new Dictionary<string, (string Key, string Value)>();
            lock (properties)
                foreach (var property in properties)
                    dict[property.Key] = (property.Key, property.Value);

            foreach (var parameter in parameters)
                dict[parameter.Key] = parameter;

            return dict.Values.ToArray();
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
