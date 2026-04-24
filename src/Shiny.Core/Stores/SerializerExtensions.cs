using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny;


public static class SerializerExtensions
{
    /// <summary>
    /// Registers a <see cref="JsonSerializerContext"/> so its types can be
    /// serialized/deserialized by <see cref="ISerializer"/> in an AOT-compatible way.
    /// </summary>
    public static IServiceCollection AddJsonContext(
        this IServiceCollection services,
        JsonSerializerContext context)
    {
        var serializer = GetOrCreateSerializer(services);
        serializer.AddContext(context);
        return services;
    }


    internal static DefaultSerializer GetOrCreateSerializer(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISerializer));
        if (descriptor?.ImplementationInstance is DefaultSerializer existing)
            return existing;

        var serializer = new DefaultSerializer();
        services.Replace(ServiceDescriptor.Singleton<ISerializer>(serializer));
        return serializer;
    }
}
