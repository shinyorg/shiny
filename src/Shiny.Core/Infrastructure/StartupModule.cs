using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Stores;


namespace Shiny.Infrastructure
{
    public class StartupModule
    {
        static readonly List<ShinyModule> modules = new List<ShinyModule>();
        public static void AddModule(ShinyModule module)
            => modules.Add(module);


        readonly IServiceProvider serviceProvider;
        readonly IObjectStoreBinder binder;
        readonly ILogger logger;


        public StartupModule(IServiceProvider serviceProvider,
                             IObjectStoreBinder binder,
                             ILogger<StartupModule> logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.binder = binder;
        }


        public async void Start(IServiceCollection services)
        {
            this.Bind(services);

            foreach (var module in modules)
                module.OnContainerReady(this.serviceProvider);

            modules.Clear();
        }


        protected void Bind(IServiceCollection services)
        {
            var singletons = services
                .Where(x =>
                    x.Lifetime == ServiceLifetime.Singleton &&
                    x.ImplementationFactory == null
                )
                .ToList();

            this.DoBindables(singletons);
            this.DoStartups(singletons);
        }


        void DoBindables(IList<ServiceDescriptor> singletons)
        {
            // check if service instance is set first
            var services = singletons
                .Where(x =>
                    x.ImplementationInstance is INotifyPropertyChanged ||
                    x.ImplementationType?.GetInterface(typeof(INotifyPropertyChanged).FullName) != null
                )
                .ToList();

            foreach (var service in services)
            {
                var instance = (INotifyPropertyChanged)this.Get(service);
                this.binder.Bind(instance);
            }
        }


        void DoStartups(IList<ServiceDescriptor> singletons)
        {
            var services = singletons
                .Where(x =>
                    x.ImplementationInstance is IShinyStartupTask ||
                    x.ImplementationType?.GetInterface(typeof(IShinyStartupTask).FullName) != null
                )
                .ToList();

            // these don't get error managed since startup errors are on the user to fix
            foreach (var service in services)
            {
                var instance = (IShinyStartupTask)this.Get(service);

                this.logger.LogInformation($"Starting up - {instance.GetType().FullName}");
                instance.Start();
                this.logger.LogInformation($"Started up - {instance.GetType().FullName}");
            }
        }


        object Get(ServiceDescriptor service)
        {
            if (service.ImplementationInstance != null)
                return service.ImplementationInstance;

            return this.serviceProvider.GetService(service.ServiceType ?? service.ImplementationType);
        }
    }
}