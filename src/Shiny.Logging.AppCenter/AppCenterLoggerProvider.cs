#if PLATFORM
using Microsoft.Extensions.Logging;

namespace Shiny.Logging.AppCenter;


public class AppCenterLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    readonly LogLevel logLevel;
    IExternalScopeProvider? scopeProvider;

    public AppCenterLoggerProvider(LogLevel logLevel) => this.logLevel = logLevel;


    public ILogger CreateLogger(string categoryName) => new AppCenterLogger(categoryName, this.logLevel, this.scopeProvider);
    public void Dispose() { }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        => this.scopeProvider = scopeProvider;
}
#endif