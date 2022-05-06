using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny
{
    public static class ShinyHost
    {
        static IServiceProvider? services;
        public static IServiceProvider ServiceProvider
        {
            get
            {
                if (services == null)
                    throw new InvalidOperationException(Constants.InitFailErrorMessage);

                return services;
            }
            internal set => services = value;
        }


        public static ILoggerFactory LoggerFactory
            => ServiceProvider.Resolve<ILoggerFactory>();


        /// <summary>
        /// Resolve a specified service from the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>() => ServiceProvider.Resolve<T>();


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Lazy<T> LazyResolve<T>() => new Lazy<T>(() => ServiceProvider.Resolve<T>());


        /// <summary>
        /// Resolve a list of registered services from the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> ResolveAll<T>()
            => ServiceProvider.GetServices<T>() ?? Enumerable.Empty<T>();

        /// <summary>
        ///
        /// </summary>
        public static bool IsInitialized => services != null;


        public static void Init(IPlatform platform, IShinyStartup? startup = null)
        {
            var services = new ServiceCollection();
            var loggingBuilder = new ShinyLoggingBuilder(services);

            services.AddSingleton<ILoggerFactory, ShinyLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(GenericLogger<>));
            services.AddSingleton(platform);

            startup?.ConfigureLogging(loggingBuilder, platform);
            startup?.ConfigureServices(services, platform);
            startup?.RegisterPlatformServices?.Invoke(services);

            //if (platform is IPlatformBuilder builder)
            //    builder.Register(services);
            //else
            //    services.RegisterCommonServices();

            ServiceProvider = startup?.CreateServiceProvider(services) ?? services.BuildServiceProvider();
            ServiceProvider.GetRequiredService<StartupModule>().Start(services);
        }
    }
}

//namespace Shiny;

//using Microsoft.Extensions.Logging;


//public static class ShinyHost
//{
//    const string InitFailErrorMessage = "ServiceProvider is not initialized - This means you have not setup Shiny correctly!  Please follow instructions at https://shinylib.net";


//    static IServiceProvider? serviceProvider;
//    public static IServiceProvider ServiceProvider
//    {
//        get
//        {
//            AssertInit();
//            return serviceProvider!;
//        }
//    }


//    static ILoggerFactory? loggerFactory;
//    public static ILoggerFactory LoggerFactory
//    {
//        get
//        {
//            AssertInit();
//            return loggerFactory!;
//        }
//    }


//    static void AssertInit()
//    {
//        if (serviceProvider == null)
//            throw new InvalidOperationException(InitFailErrorMessage);
//    }


//    public static void Init(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
//    {
//        if (serviceProvider != null)
//            throw new InvalidOperationException("ShinyHost is already initialized");

//        serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
//        loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
//    }


//    public static void Clear()
//    {
//        serviceProvider = null;
//        loggerFactory = null;
//        // dispose here or some sort of manual shiny shutdown?
//    }
//}