using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Jobs;
using Shiny.Stores;


namespace Shiny.Infrastructure
{
    public class StartupModule
    {
        static readonly List<ShinyModule> modules = new List<ShinyModule>();
        public static void AddModule(ShinyModule module)
            => modules.Add(module);


        static readonly List<JobInfo> jobs = new List<JobInfo>();
        public static void AddJob(JobInfo jobInfo) => jobs.Add(jobInfo);


        readonly IJobManager jobManager;
        readonly IServiceProvider serviceProvider;
        readonly IObjectStoreBinder binder;
        readonly ILogger logger;


        public StartupModule(IJobManager jobManager,
                             IServiceProvider serviceProvider,
                             IObjectStoreBinder binder,
                             ILogger<StartupModule> logger)
        {
            this.jobManager = jobManager;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.binder = binder;
        }


        public async void Start(IServiceCollection services)
        {
            this.Bind(services);

            if (jobs.Count > 0)
            {
                var access = await this.jobManager.RequestAccess();
                if (access == AccessState.Available)
                {
                    foreach (var job in jobs)
                        await this.jobManager.Register(job);
                }
                else
                {
                    this.logger.LogWarning("Permissions to run jobs insufficient: " + access);
                }
            }

            foreach (var module in modules)
                module.OnContainerReady(this.serviceProvider);

            jobs.Clear();
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
        }


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

//    public void Configure(ShinyOptions options)
//        public Task StartAsync(CancellationToken cancellationToken)
//    {
//        var objectBinder = this.serviceProvider.GetRequiredService<IObjectStoreBinder>();

//        var singletons = options
//            var singletons = this.options
//                .Value
//                .Services
//                .Where(x =>
//                    x.Lifetime == ServiceLifetime.Singleton &&
//@ -44, 7 + 48, 7 @@ namespace Shiny
//{
//                if (service.ImplementationInstance != null)
//                {
//                    this.TryRun(service.ImplementationInstance, objectBinder);
//                    this.TryRun(service.ImplementationInstance);
//                }
//                else
//                {
//@ -55,21 +59,26 @@ namespace Shiny
//    {
//        // downfall here is that all INPC's are also getting warmed up right away
//        var instance = this.serviceProvider.GetService(service.ServiceType ?? service.ImplementationType);
//                        this.TryRun(instance, objectBinder);
//                        this.TryRun(instance);
//                    }
//    }
//}
//return Task.CompletedTask;
//        }


//        void TryRun(object instance, IObjectStoreBinder binder)
//        public Task StopAsync(CancellationToken cancellationToken)
//            => Task.CompletedTask;


//void TryRun(object instance)
//{
//    // TODO: I may not want to do this in MAUI since the viewmodels may live on the container
//    if (instance is INotifyPropertyChanged npc)
//    {
//        try
//        {
//            binder.Bind(npc);
//            this.binder.Bind(npc);
//            this.logger.LogInformation($"Successfully bound model - {instance.GetType().FullName}");
//        }
//        catch (Exception ex)
//@ -86,4 + 95,4 @@ namespace Shiny
//            }
//        }
//    }