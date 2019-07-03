using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
using Shiny.Settings;


namespace Shiny
{
    public static partial class Extensions
    {
        static readonly List<Action<IServiceProvider>> postBuildActions = new List<Action<IServiceProvider>>();


        internal static void RunPostBuildActions(this IServiceProvider container)
        {
            foreach (var action in postBuildActions)
                action(container);

            postBuildActions.Clear();
        }


        /// <summary>
        /// Registers a post container build step
        /// </summary>
        /// <param name="services"></param>
        /// <param name="action"></param>
        public static void RegisterPostBuildAction(this IServiceCollection services, Action<IServiceProvider> action)
            => postBuildActions.Add(action);



        /// <summary>
        /// Registers a shiny singleton - If it implements IStartupTask
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddService<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddSingleton<TService>(sp =>
            {
                var instance = sp.ResolveOrInstantiate<TImplementation>();
                if (instance is INotifyPropertyChanged npc)
                    // attribute to get bindable point
                    sp.GetService<ISettings>().Bind(npc);

                return instance;
            });
            if (typeof(TService).IsAssignableFrom(typeof(IStartupTask)) ||
                typeof(TImplementation).IsAssignableFrom(typeof(IStartup)))
            {
                postBuildActions.Add(sp =>
                    ((IStartupTask)sp.GetService<TService>()).Start()
                );
            }
        }


        /// <summary>
        /// Register a startup task that runs immediately after the container is built with full dependency injected services
        /// </summary>
        /// <param name="services"></param>
        public static void RegisterStartupTask<TImplementation>(this IServiceCollection services)
            where TImplementation : IStartupTask

            => services.RegisterPostBuildAction(sp =>
            {
                var instance = sp.ResolveOrInstantiate<TImplementation>();
                if (instance is INotifyPropertyChanged npc)
                    sp.GetService<ISettings>().Bind(npc);

                instance.Start();
            });


        /// <summary>
        /// Register a job on the job manager
        /// </summary>
        /// <param name="services"></param>
        /// <param name="jobInfo"></param>
        public static void RegisterJob(this IServiceCollection services, JobInfo jobInfo)
            => services.RegisterPostBuildAction(async sp =>
            {
                // what if permission fails?
                var jobs = sp.GetService<IJobManager>();
                var access = await jobs.RequestAccess();
                if (access == AccessState.Available)
                    await jobs.Schedule(jobInfo);
            });


        /// <summary>
        /// Attempts to resolve or build an instance from a service provider
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static object ResolveOrInstantiate(this IServiceProvider services, Type type)
            => ActivatorUtilities.GetServiceOrCreateInstance(services, type);


        /// <summary>
        /// Attempts to resolve or build an instance from a service provider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static T ResolveOrInstantiate<T>(this IServiceProvider services)
            => (T)ActivatorUtilities.GetServiceOrCreateInstance(services, typeof(T));


        /// <summary>
        /// Register a module (like a category) of services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="module"></param>
        public static void RegisterModule(this IServiceCollection services, IModule module)
        {
            module.Register(services);
            services.RegisterPostBuildAction(module.OnContainerReady);
        }


        /// <summary>
        /// Register a module (like a category) of services
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        public static void RegisterModule<T>(this IServiceCollection services)
            where T : IModule, new() => services.RegisterModule(new T());


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

            services.Add(new ServiceDescriptor(
                typeof(TService),
                typeof(TImpl),
                desc.Lifetime
            ));
        }


        /// <summary>
        /// Add or replace a service registration
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <param name="instance">The singleton instance you wish to use</param>
        public static void AddOrReplace<TService>(this IServiceCollection services, TService instance)
        {
            var desc = services.SingleOrDefault(x => x.ServiceType == typeof(TService));
            if (desc != null)
                services.Remove(desc);

            services.Add(new ServiceDescriptor(typeof(TService), instance));
        }


        /// <summary>
        /// Add or replace a service registration
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <param name="instance"></param>
        public static void AddOrReplace<TService>(this IServiceCollection services, Func<IServiceProvider, TService> factory)
        {
            var desc = services.SingleOrDefault(x => x.ServiceType == typeof(TService));
            if (desc != null)
                services.Remove(desc);

            services.Add(new ServiceDescriptor(typeof(TService), factory));
        }


        /// <summary>
        /// Regiseter a service on the collection if it one is not already registered
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="services"></param>
        /// <param name="lifetime"></param>
        public static void AddIfNotRegistered<TService, TImpl>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (!services.IsRegistered<TService>())
                services.Add(new ServiceDescriptor(typeof(TService), typeof(TImpl), lifetime));
        }



        /// <summary>
        /// Regiseter a service on the collection if it one is not already registered
        /// </summary>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="services"></param>
        /// <param name="lifetime"></param>
        public static void AddIfNotRegistered<TImpl>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (!services.IsRegistered<TImpl>())
                services.Add(new ServiceDescriptor(typeof(TImpl), lifetime));
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
