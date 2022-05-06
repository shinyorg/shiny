using System;
//using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Hosting.Impl;

namespace Shiny.Hosting;


public class Host : IHost
{
    // TODO: put static instance here

    public IServiceProvider ServiceProvider { get; init; }
    //public IConfiguration Configuration { get; init; }
    public ILoggerFactory Logging { get; init;}
    public IPlatform Platform { get; init; }


    public static IHostBuilder CreateDefault()
    {
        var builder = new HostBuilder();

        return builder;
    }
}
