using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Data.Sync;
using Shiny.Data.Sync.Infrastructure;
using Shiny.Jobs;

namespace Shiny;


public static class DataSyncServiceCollectionExtensions
{
    /// <summary>
    /// Registers Shiny.Data.Sync with the native background transport for the current target
    /// (NSURLSession on Apple, Foreground Service on Android, HttpClient on Windows/Linux/base .NET).
    /// </summary>
    /// <typeparam name="TDelegate">App-provided implementation of <see cref="IDataSyncDelegate"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Endpoint registration callback.</param>
    public static IServiceCollection AddDataSync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(
        this IServiceCollection services,
        Action<ISyncEndpointBuilder> configure
    )
        where TDelegate : class, IDataSyncDelegate
    {
#if IOS || MACCATALYST || TVOS
        services.AddSingletonAsImplementedInterfaces<Shiny.Data.Sync.DataSyncManager>();
#elif ANDROID
        services.AddSingletonAsImplementedInterfaces<Shiny.Data.Sync.DataSyncManager>();
        services.AddSingleton<HttpClientDataSyncProcess>();
#else
        services.AddSingletonAsImplementedInterfaces<HttpClientDataSyncManager>();
        services.AddSingleton<HttpClientDataSyncProcess>();
#endif
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        AddCore(services, configure);
        return services;
    }


    /// <summary>
    /// Registers the cross-platform HttpClient-backed data sync manager. Use this explicitly
    /// when you want the HttpClient path on a platform that would otherwise pick a native
    /// transport (e.g., running on iOS but you don't want NSURLSession).
    /// </summary>
    public static IServiceCollection AddHttpClientDataSync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(
        this IServiceCollection services,
        Action<ISyncEndpointBuilder> configure
    )
        where TDelegate : class, IDataSyncDelegate
    {
        services.AddSingletonAsImplementedInterfaces<HttpClientDataSyncManager>();
        services.AddSingleton<HttpClientDataSyncProcess>();
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        AddCore(services, configure);
        return services;
    }


    /// <summary>
    /// Registers a global <see cref="ISyncInterceptor"/> that runs before every outbox / inbox / tombstone
    /// HTTP request. Multiple interceptors may be registered — they execute in registration order.
    /// </summary>
    public static IServiceCollection AddSyncInterceptor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TInterceptor>(
        this IServiceCollection services
    )
        where TInterceptor : class, ISyncInterceptor
    {
        services.AddSingleton<ISyncInterceptor, TInterceptor>();
        return services;
    }


    static void AddCore(IServiceCollection services, Action<ISyncEndpointBuilder> configure)
    {
#if PLATFORM
        services.AddConnectivity();
#endif
        services.AddDefaultRepository();

        var registry = new SyncEndpointRegistry();
        configure(registry);
        services.AddSingleton(registry);

        // Register the named HttpClient used by RestSyncTransport. Apps can configure it
        // further with .ConfigureHttpClient(...) - we only fill the gap if no one else does.
        services.AddHttpClient(RestSyncTransport.HttpClientName);

        services.AddSingleton<ISyncTransport, RestSyncTransport>();
        services.AddSingleton<SyncInboxProcessor>();
        services.AddJob<SyncJob>(r => r.WithInternet(InternetAccess.Any));
    }
}
