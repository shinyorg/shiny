using System;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;


/// <summary>
/// Represents the Shiny host that manages services and application lifecycle
/// </summary>
public interface IHost : IDisposable
{
    /// <summary>
    /// Gets the service provider for resolving dependencies
    /// </summary>
    IServiceProvider Services { get; }
    //IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the logging factory for creating loggers
    /// </summary>
    ILoggerFactory Logging { get; }

    /// <summary>
    /// Runs the host and starts all registered services
    /// </summary>
    void Run();
}
