using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;
using Shiny.Infrastructure.DependencyInjection;


namespace Shiny
{
    public static class ShinyHost
    {
        /// <summary>
        /// Resolve a specified service from the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>() => Container.Resolve<T>();


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Lazy<T> LazyResolve<T>() => new Lazy<T>(() => Container.Resolve<T>());


        /// <summary>
        /// Resolve a list of registered services from the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> ResolveAll<T>()
            => Container.GetServices<T>() ?? Enumerable.Empty<T>();


        /// <summary>
        /// The configured service collection post container set
        /// </summary>
        public static IServiceCollection? Services { get; private set; }


        /// <summary>
        /// Feeds another service container
        /// </summary>
        /// <param name="feed"></param>
        public static void Populate(Action<Type, Func<object>, ServiceLifetime> feed)
        {
            if (Services == null)
                throw new ArgumentException("Services have not yet been initialized");

            foreach (var service in Services)
            {
                if (!service.ServiceType.IsGenericTypeDefinition)
                {
                    feed(
                        service.ServiceType,
                        () => Container.GetService(service.ServiceType),
                        service.Lifetime
                    );
                }
            }
        }


        static IServiceProvider? container;
        public static IServiceProvider Container
        {
            get
            {
                if (container == null)
                    throw new ArgumentException("Container has not been setup - have you initialized the Platform provider using ShinyHost.Init?");

                return container;
            }
        }


        public static ILoggerFactory LoggerFactory
            => Container.Resolve<ILoggerFactory>();


        /// <summary>
        /// Setting this before calling build will force the internal service builder to validate scopes of DI registrations (THIS IS SLOW - USE IT FOR DEBUGGING)
        /// </summary>
        public static bool ValidateScopes { get; set; }


        public static void Init(IPlatform platform, IShinyStartup? startup = null)
        {
            var services = new ShinyServiceCollection();
            services.AddSingleton(platform);
            //services.AddOptions();
            services.AddLogging(builder => startup?.ConfigureLogging(builder, platform));
            //services.AddSingleton(typeof(ILogger<>), typeof(DummyLogger<>));

            if (startup != null)
            {
                if (platform is IStartupInitializer startupInitializer)
                    startupInitializer.Initialize(startup, services);
                else
                    startup.ConfigureServices(services, platform);
            }
            platform.Register(services);

            Services = services;
            //var sw = new System.Diagnostics.Stopwatch();
            //sw.Start();

            services.BuildShinyServiceProvider(
                ValidateScopes,
                s => startup?.CreateServiceProvider(s),
                s => container = s
            );
            //sw.Stop();
            //Console.WriteLine($"Startup: {sw.ElapsedMilliseconds} ms");
        }
    }

    //public class DummyLogger<T> : ILogger, IDisposable
    //{
    //    public IDisposable BeginScope<TState>(TState state) => this;
    //    public void Dispose() { }
    //    public bool IsEnabled(LogLevel logLevel) => true;
    //    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
    //}
}