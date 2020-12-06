using System;
using Shiny.Settings;
using Shiny.Jobs;


namespace Shiny.Infrastructure
{
    public class ShinyCoreServices
    {
#if __ANDROID__
        public ShinyCoreServices(IAndroidContext context,
                                 ISettings settings,
                                 IRepository repository,
                                 IServiceProvider services,
                                 ISerializer serializer,
                                 IMessageBus bus,
                                 IJobManager jobManager)
            : this(settings, repository, services, serializer, bus, jobManager)
        {
            this.Android = context;
        }

#endif
        public ShinyCoreServices(ISettings settings,
                                 IRepository repository,
                                 IServiceProvider services,
                                 ISerializer serializer,
                                 IMessageBus bus,
                                 IJobManager jobManager)
        {
            this.Settings = settings;
            this.Repository = repository;
            this.Services = services;
            this.Serializer = serializer;
            this.Bus = bus;
            this.Jobs = jobManager;
        }

        #if __ANDROID__
        public IAndroidContext Android { get; }
        #endif
        public ISettings Settings { get; }
        public IRepository Repository { get; }
        public IServiceProvider Services { get; }
        public ISerializer Serializer { get; }
        public IMessageBus Bus { get; }
        public IJobManager Jobs { get; }
    }
}
