using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Stores;


namespace Shiny
{
    internal class ShinyOptions
    {
        public IServiceCollection? Services { get; set; }
    }


    internal class ConfigureShinyOptions : IConfigureOptions<ShinyOptions>
    {
        readonly IServiceProvider serviceProvider;
        readonly ILogger logger;


        public ConfigureShinyOptions(IServiceProvider serviceProvider, ILogger<ConfigureShinyOptions> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }


        public void Configure(ShinyOptions options)
        {
            // TODO: modules post configure
            var objectBinder = this.serviceProvider.GetRequiredService<IObjectStoreBinder>();

            var singletons = options
                .Services
                .Where(x =>
                    x.Lifetime == ServiceLifetime.Singleton &&
                    x.ImplementationFactory == null
                )
                .ToList();

            foreach (var service in singletons)
            {
                if (service.ImplementationInstance != null)
                {
                    this.TryRun(service.ImplementationInstance, objectBinder);
                }
                else
                {
                    // before resolve, ensure implementationtype implements
                    var shouldRun = service.ImplementationType.GetInterface(typeof(INotifyPropertyChanged).FullName) != null ||
                                    service.ImplementationType.GetInterface(typeof(IShinyStartupTask).FullName) != null;
                    if (shouldRun)
                    {
                        // downfall here is that all INPC's are also getting warmed up right away
                        var instance = this.serviceProvider.GetService(service.ServiceType ?? service.ImplementationType);
                        this.TryRun(instance, objectBinder);
                    }
                }
            }
        }


        void TryRun(object instance, IObjectStoreBinder binder)
        {
            // TODO: I may not want to do this in MAUI since the viewmodels may live on the container
            if (instance is INotifyPropertyChanged npc)
            {
                try
                {
                    binder.Bind(npc);
                    this.logger.LogInformation($"Successfully bound model - {instance.GetType().FullName}");
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Failed to bind stateful model - {instance.GetType().FullName}");
                }
            }

            if (instance is IShinyStartupTask startup)
            {
                this.logger.LogInformation($"Starting up - {instance.GetType().FullName}");
                startup.Start();
                this.logger.LogInformation($"Started up - {instance.GetType().FullName}");
            }
        }
    }
}