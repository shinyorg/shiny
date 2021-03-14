using System;
using Shiny.Jobs;
using Shiny.Stores;


namespace Shiny.Infrastructure
{
    public class ShinyCoreServices
    {
#if __ANDROID__
        public ShinyCoreServices(IAndroidContext context,
                                 IKeyValueStoreFactory store,
                                 IRepository repository,
                                 IServiceProvider services,
                                 ISerializer serializer,
                                 IMessageBus bus,
                                 IJobManager jobManager)
            : this(store, repository, services, serializer, bus, jobManager)
        {
            this.Android = context;
        }

#endif
        public ShinyCoreServices(IKeyValueStoreFactory store,
                                 IRepository repository,
                                 IServiceProvider services,
                                 ISerializer serializer,
                                 IMessageBus bus,
                                 IJobManager jobManager)
        {
            this.Settings = store.DefaultStore;
            this.Repository = repository;
            this.Services = services;
            this.Serializer = serializer;
            this.Bus = bus;
            this.Jobs = jobManager;
        }

        #if __ANDROID__
        public IAndroidContext Android { get; }
        #endif
        public IKeyValueStore Settings { get; }
        public IRepository Repository { get; }
        public IServiceProvider Services { get; }
        public ISerializer Serializer { get; }
        public IMessageBus Bus { get; }
        public IJobManager Jobs { get; }
    }
}
