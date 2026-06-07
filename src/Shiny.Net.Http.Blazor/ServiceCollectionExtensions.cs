using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Http;
using BlazorHttpTransferManager = Shiny.Net.Http.Blazor.HttpTransferManager;
using Shiny.Net.Http.Blazor;

namespace Shiny;


public static class BlazorHttpServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Blazor WASM HTTP transfer manager backed by the Service Worker
    /// Background Sync API and IndexedDB. Transfers continue to drain in the
    /// background while the tab is closed (where supported by the browser).
    ///
    /// Your app must serve the Shiny SW script (or import its handlers from your own
    /// service worker). The default path is
    /// <c>./_content/Shiny.Net.Http.Blazor/http-transfer-sw.js</c>.
    /// </summary>
    public static IServiceCollection AddBlazorHttpTransfers<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDelegate>(
        this IServiceCollection services,
        Action<BlazorHttpTransferOptions>? configure = null
    )
        where TDelegate : class, IHttpTransferDelegate
    {
        var options = new BlazorHttpTransferOptions();
        configure?.Invoke(options);

        services.AddBattery();
        services.AddConnectivity();
        services.AddLocalStorageRepository();

        services.AddSingleton(options);
        services.AddSingleton<BlazorHttpTransferManager>();
        services.AddSingleton<IHttpTransferManager>(sp => sp.GetRequiredService<BlazorHttpTransferManager>());

        services.AddSingleton<TDelegate>();
        services.AddSingleton<IHttpTransferDelegate>(sp => sp.GetRequiredService<TDelegate>());
        return services;
    }
}
