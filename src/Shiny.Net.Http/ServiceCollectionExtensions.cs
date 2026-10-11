using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Hosting;
using Shiny.Net.Http;
using Shiny.Net.Http.Infrastructure;

namespace Shiny;


public static class HttpTransferServiceCollectionExtensions
{
    public static IServiceCollection AddHttpTransfers<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services)
        where TDelegate : class, IHttpTransferDelegate
    {
#if IOS || MACCATALYST || TVOS
        services.AddSingletonAsImplementedInterfaces<HttpTransferManager>();
#elif ANDROID
        // Register the Android platform HttpTransferManager so transfers run inside
        // the dataSync foreground service (HttpTransferService) the package ships.
        // Registering HttpClientHttpTransferManager here instead runs the transfer
        // loop as an in-process Task.Run: transfers don't survive backgrounding and
        // the loop's isRunning flag stalls after its first batch ("N uploaded then
        // everything stuck Pending"). The FGS still resolves
        // HttpClientHttpTransferProcess + the named HttpClient from DI to move the
        // bytes, so those registrations stay.
        services.AddSingletonAsImplementedInterfaces<HttpTransferManager>();
        services.AddSingleton<HttpClientHttpTransferProcess>();
        services.AddHttpClient(HttpClientHttpTransferProcess.HttpClientName);
#elif WINDOWS
        services.AddSingletonAsImplementedInterfaces<HttpClientHttpTransferManager>();
        services.AddSingleton<HttpClientHttpTransferProcess>();
        services.AddHttpClient(HttpClientHttpTransferProcess.HttpClientName);
#endif
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        AddCore(services);

        return services;
    }
    
    
    /// <summary>
    /// Registers the Shiny.Net.Http background transfer manager backed by a managed
    /// HttpClient + IConnectivity loop. Supports resumable downloads (uploads always
    /// restart). Use this on Linux, macOS server, Blazor, and other plain .NET targets.
    ///
    /// An <see cref="Shiny.Net.IConnectivity"/> is registered for you (register your own first to
    /// replace it). A default JSON filesystem repository is registered automatically under
    /// {LocalApplicationData}/Shiny.
    /// </summary>
    public static IServiceCollection AddHttpClientTransfers<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services)
        where TDelegate : class, IHttpTransferDelegate
    {
        services.AddSingletonAsImplementedInterfaces<HttpClientHttpTransferManager>();
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        services.AddSingleton<HttpClientHttpTransferProcess>();
        services.AddHttpClient(HttpClientHttpTransferProcess.HttpClientName);

        AddCore(services);
        return services;
    }


#if !PLATFORM
    /// <summary>
    /// Registers the Blazor WebAssembly HTTP transfer manager backed by the Service Worker
    /// Background Sync API and IndexedDB. Transfers continue to drain in the
    /// background while the tab is closed (where supported by the browser).
    ///
    /// Your app must serve the Shiny SW script (or import its handlers from your own
    /// service worker). The default path is
    /// <c>./_content/Shiny.Net.Http/http-transfer-sw.js</c>.
    /// </summary>
    public static IServiceCollection AddBlazorHttpTransfers<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(
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
        services.AddSingletonAsImplementedInterfaces<BrowserHttpTransferManager>();
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        return services;
    }
#endif


    static void AddCore(IServiceCollection services)
    {
        services.AddConnectivity();
        services.AddDefaultRepository();
        services.AddSingleton<HttpTransferMonitor>();
    }
}
