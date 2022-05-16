using System;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;

public interface IHost
{
    IServiceProvider ServiceProvider { get; }
    //IConfiguration Configuration { get; }
    ILoggerFactory Logging { get; }

    //void Run();
}
