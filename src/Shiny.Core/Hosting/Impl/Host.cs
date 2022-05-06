using System;
//using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting.Impl;


public class Host : IHost
{
    public IServiceProvider ServiceProvider { get; init; }
    //public IConfiguration Configuration { get; init; }
    public ILoggerFactory Logging { get; init;}
    public IPlatform Platform { get; init; }
}
