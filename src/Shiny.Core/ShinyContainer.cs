using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny;


public class ShinyContainer
{

    static ShinyContainer? current;
    public static ShinyContainer Current
    {
        get
        {
            if (current == null)
                throw new InvalidOperationException("ShinyContainer does not appear to be initialized.  Make sure to call Init first");

            return current;
        }
    }


    public T GetService<T>() => this.Services.GetRequiredService<T>()!;
    public ILoggerFactory Logging => this.Host.Logging;
    public IServiceProvider Services => this.Host.Services;
    public IHost Host { get; init; }


    /// <summary>
    /// Initialize a new static container - not recommended for use with MAUI or any type of DI centric framework like Prism
    /// </summary>
    /// <param name="buildAction"></param>
    public static void Init(Action<IHostBuilder> buildAction)
    {
        var builder = new HostBuilder();
        buildAction(builder);
        var host = builder.Build();
        host.Run();

        current = new ShinyContainer
        {
            Host = host
        };
    }
}
