using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.Infrastructure.Impl;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny;


public static class HostExtensions
{
    public static void AddCommon(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
        services.TryAddSingleton<IMessageBus, MessageBus>();
    }
}