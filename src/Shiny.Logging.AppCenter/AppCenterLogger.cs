#if PLATFORM
using System;
using System.Collections.Generic;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

namespace Shiny.Logging.AppCenter;


public class AppCenterLogger : ILogger
{
    readonly string categoryName;
    readonly IExternalScopeProvider? scopeProvider;
    readonly LogLevel configLogLevel;


    public AppCenterLogger(string categoryName, LogLevel logLevel, IExternalScopeProvider? scopeProvider)
    {
        this.categoryName = categoryName;
        this.configLogLevel = logLevel;
        this.scopeProvider = scopeProvider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => this.scopeProvider?.Push(state);

    public bool IsEnabled(LogLevel logLevel) => logLevel >= this.configLogLevel;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!this.IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var scopeVars = new Dictionary<string, string>
        {
            { "Message", message }
        };

        this.scopeProvider?.ForEachScope((scope, dict) =>
        {
            if (scope is IEnumerable<KeyValuePair<string, object>> properties)
            {
                foreach (var pair in properties)
                {
                    dict.Add(pair.Key, pair.Value.ToString() ?? String.Empty);
                }
            }
            else if (scope != null)
            {
                dict.Add("Scope", scope.ToString()!);
            }
        }, scopeVars);

        if (logLevel <= LogLevel.Information || exception == null)
        {
            Analytics.TrackEvent(
                this.categoryName,
                scopeVars
            );
        }
        else
        {
            Crashes.TrackError(
                exception,
                scopeVars
            );
        }
    }
}
#endif