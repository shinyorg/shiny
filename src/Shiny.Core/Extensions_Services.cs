using System;
using System.ComponentModel;
using System.Linq;
using Shiny.Settings;
using Shiny.Jobs;
using Shiny.Caching;
using Shiny.Infrastructure;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public static class Extensions_Services
    {
        /// <summary>
        /// Adds an injectable (ICache) cache service that doesn't actually cache at all - good for testing
        /// </summary>
        /// <param name="services"></param>
        public static void UseVoidCache(this IServiceCollection services)
            => services.AddSingleton<ICache, VoidCache>();


        /// <summary>
        /// Adds an injectable (ICache) in-memory cache
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="defaultLifespan">The default timespan for how long objects should live in cache if time is not explicitly set</param>
        /// <param name="cleanUpTimer">The internal cleanup time interval (don't make this too big or too small)</param>
        public static void UseMemoryCache(this IServiceCollection services,
                                          TimeSpan? defaultLifespan = null,
                                          TimeSpan? cleanUpTimer = null)
            => services.AddSingleton<ICache>(_ => new MemoryCache(defaultLifespan, cleanUpTimer));


        /// <summary>
        /// Uses the built-in repository (default is file based) to store cache data
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="defaultLifespan">The default timespan for how long objects should live in cache if time is not explicitly set</param>
        /// <param name="cleanUpTimer">The internal cleanup time interval (don't make this too big or too small)</param>
        public static void UseRepositoryCache(this IServiceCollection services,
                                              TimeSpan? defaultLifespan = null,
                                              TimeSpan? cleanUpTimer = null)
            => services.AddSingleton<ICache>(sp =>
            {
                var repository = sp.GetRequiredService<IRepository>();
                return new RepositoryCache(repository, defaultLifespan, cleanUpTimer);
            });


        /// <summary>
        /// Register a strongly typed application settings provider on the service container
        /// </summary>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="services"></param>
        /// <param name="prefix"></param>
        public static void RegisterSettings<TImpl>(this IServiceCollection services, string prefix = null)
                where TImpl : class, INotifyPropertyChanged, new()
            => services.RegisterSettings<TImpl, TImpl>(prefix);


        /// <summary>
        /// Register a strongly typed application settings provider on the service container with a service interface
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="services"></param>
        /// <param name="prefix"></param>
        public static void RegisterSettings<TService, TImpl>(this IServiceCollection services, string prefix = null)
                where TService : class
                where TImpl : class, TService, INotifyPropertyChanged, new()
            => services.AddSingleton<TService>(c => c
                .GetService<ISettings>()
                .Bind<TImpl>(prefix)
            );


        /// <summary>
        /// Register a startup task that runs immediately after the container is built with full dependency injected services
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        public static void RegisterStartupTask<T>(this IServiceCollection services) where T : class, IStartupTask
            => services.AddSingleton<IStartupTask, T>();


        /// <summary>
        /// Register a job on the job manager
        /// </summary>
        /// <param name="services"></param>
        /// <param name="jobInfo"></param>
        public static void RegisterJob(this IServiceCollection services, JobInfo jobInfo)
        {
            if (!services.Any(x => x.ImplementationType == typeof(PostRegisterTask)))
                services.AddSingleton<IStartupTask, PostRegisterTask>();

            PostRegisterTask.Jobs.Add(jobInfo);
        }


        /// <summary>
        /// Add or replace a service registration
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="services"></param>
        public static void AddOrReplace<TService, TImpl>(this IServiceCollection services)
        {
            var desc = services.SingleOrDefault(x => x.ServiceType == typeof(TService));
            if (desc != null)
                services.Remove(desc);

            services.Add(new ServiceDescriptor(typeof(TService), typeof(TImpl), desc.Lifetime));
        }


        /// <summary>
        /// Regiseter a service on the collection if it one is not already registered
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="services"></param>
        /// <param name="lifetime"></param>
        public static void AddIfNotRegister<TService, TImpl>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (!services.IsRegistered<TService>())
                services.Add(new ServiceDescriptor(typeof(TService), typeof(TImpl), lifetime));
        }


        /// <summary>
        /// Check if a service type is registered on the service builder
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static bool IsRegistered<TService>(this IServiceCollection services)
            => services.Any(x => x.ServiceType == typeof(TService));


        /// <summary>
        /// Check if a service is registered in the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static bool IsRegistered<T>(this IServiceProvider services)
            => services.GetService(typeof(T)) != null;


        /// <summary>
        /// Check if a service is register in the container
        /// </summary>
        /// <param name="services"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static bool IsRegistered(this IServiceProvider services, Type serviceType)
            => services.GetService(serviceType) != null;
    }
}
