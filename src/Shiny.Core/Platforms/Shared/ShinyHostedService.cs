using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny.Stores;


namespace Shiny
{
    class ShinyHostedService : IHostedService
    {
        readonly IOptions<ShinyOptions> options;
        readonly IServiceProvider serviceProvider;
        readonly IObjectStoreBinder binder;
        readonly ILogger logger;


        public ShinyHostedService(IOptions<ShinyOptions> options,
                                  IServiceProvider serviceProvider,
                                  ILogger<ShinyHostedService> logger,
                                  IObjectStoreBinder binder)
        {
            ShinyHost.ServiceProvider = serviceProvider;
            this.options = options;
            this.serviceProvider = serviceProvider;
            this.binder = binder;
            this.logger = logger;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            var singletons = this.options
                .Value
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
                    this.TryRun(service.ImplementationInstance);
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
                        this.TryRun(instance);
                    }
                }
            }
            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;


        void TryRun(object instance)
        {
            // TODO: I may not want to do this in MAUI since the viewmodels may live on the container
            if (instance is INotifyPropertyChanged npc)
            {
                try
                {
                    this.binder.Bind(npc);
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
