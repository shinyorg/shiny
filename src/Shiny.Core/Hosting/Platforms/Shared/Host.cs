using System;

using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;


public abstract class Host : IHost
{
    const string InitFailErrorMessage = "ServiceProvider is not initialized - This means you have not setup Shiny correctly!  Please follow instructions at https://shinylib.net";


    static IHost? currentHost;
    public static IHost Current
    {
        get
        {
            if (currentHost == null)
                throw new InvalidOperationException(InitFailErrorMessage);

            return currentHost;
        }
        internal set
        {
            ArgumentNullException.ThrowIfNull(value);
            currentHost = value;
        }
    }

    public static bool IsInitialized => currentHost != null;


    public IServiceProvider ServiceProvider { get; init; }
    //public IConfiguration Configuration { get; init; }
    public ILoggerFactory Logging { get; init; }


    public static IHostBuilder CreateDefault()
    {
        //var builder = new HostBuilder(null);

        //return builder;
        return null;
    }
}
//public static void Init(IPlatform platform)
//{
//    var services = new ServiceCollection();
//    var loggingBuilder = new ShinyLoggingBuilder(services);

//    services.AddSingleton<ILoggerFactory, ShinyLoggerFactory>();
//    services.AddSingleton(typeof(ILogger<>), typeof(GenericLogger<>));
//    services.AddSingleton(platform);

//    //startup?.ConfigureLogging(loggingBuilder, platform);
//    //startup?.ConfigureServices(services, platform);
//    //startup?.RegisterPlatformServices?.Invoke(services);

//    ////if (platform is IPlatformBuilder builder)
//    ////    builder.Register(services);
//    ////else
//    ////    services.RegisterCommonServices();

//    //ServiceProvider = startup?.CreateServiceProvider(services) ?? services.BuildServiceProvider();
//    ServiceProvider.GetRequiredService<StartupModule>().Start(services);
//}