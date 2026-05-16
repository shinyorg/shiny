using Microsoft.Extensions.DependencyInjection;
using Shiny.Net.Http;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpTransfers<TDelegate>(this IServiceCollection services)
        where TDelegate : class, IHttpTransferDelegate
    {
        services.AddConnectivity();

        services.AddSingleton<HttpTransferManager>();
        services.AddSingleton<IHttpTransferManager>(sp => sp.GetRequiredService<HttpTransferManager>());
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<HttpTransferManager>());
#if IOS || MACCATALYST
        services.AddSingleton<IIosLifecycle.IHandleEventsForBackgroundUrl>(sp => sp.GetRequiredService<HttpTransferManager>());
#endif

        services.AddSingleton<TDelegate>();
        services.AddSingleton<IHttpTransferDelegate>(sp => sp.GetRequiredService<TDelegate>());
        services.AddDefaultRepository();
        services.AddJsonContext(ShinyHttpJsonContext.Default);
        services.AddSingleton<HttpTransferMonitor>();

#if ANDROID || WINDOWS
        services.AddSingleton<HttpTransferProcess>();
#endif
        return services;
    }
}
