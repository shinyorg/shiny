using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Data.Sync;
using Shiny.Data.Sync.Infrastructure;

namespace Shiny;


public static class BlazorDataSyncServiceCollectionExtensions
{
    /// <summary>
    /// Registers Shiny.Data.Sync for Blazor WASM, persisting the outbox and inbox cursors
    /// to LocalStorage and draining via in-process HttpClient driven by the connectivity loop.
    ///
    /// Note: this does NOT use Service Worker Background Sync — sync runs while the tab is open.
    /// A future Shiny.Data.Sync.Blazor.ServiceWorker package will add the same true-background
    /// guarantees that Shiny.Net.Http.Blazor provides for file transfers.
    /// </summary>
    public static IServiceCollection AddBlazorDataSync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(
        this IServiceCollection services,
        Action<ISyncEndpointBuilder> configure
    )
        where TDelegate : class, IDataSyncDelegate
    {
        services.AddBattery();
        services.AddConnectivity();
        services.AddLocalStorageRepository();

        services.AddSingletonAsImplementedInterfaces<HttpClientDataSyncManager>();
        services.AddSingleton<HttpClientDataSyncProcess>();
        services.AddSingletonAsImplementedInterfaces<TDelegate>();

        var registry = new SyncEndpointRegistry();
        configure(registry);
        services.AddSingleton(registry);

        services.AddHttpClient(RestSyncTransport.HttpClientName);
        services.AddSingleton<ISyncTransport, RestSyncTransport>();
        services.AddSingleton<SyncInboxProcessor>();
        return services;
    }
}
