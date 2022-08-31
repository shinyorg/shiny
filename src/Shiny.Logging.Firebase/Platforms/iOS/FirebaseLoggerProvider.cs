using System;
using Microsoft.Extensions.Logging;

namespace Shiny.Logging.Firebase;


public class FirebaseLoggerProvider : ILoggerProvider
{
    readonly LogLevel logLevel;
    public FirebaseLoggerProvider(LogLevel logLevel) => this.logLevel = logLevel;

    public ILogger CreateLogger(string categoryName) => new FirebaseLogger(categoryName, this.logLevel);
    public void Dispose() { }
}