#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Shiny.Hosting;

namespace Shiny.Infrastructure;


public static class ShinyInfrastructureExtensions
{
    /// <summary>
    /// This is called by Shiny hosting - You should NOT be calling this yourself
    /// </summary>
    public static IServiceCollection AddShinyCoreServices(this IServiceCollection services)
    {
        services.AddShinyStores();

#if ANDROID
        services.AddSingleton<AndroidPlatform>();
        services.AddSingleton<IPlatform>(sp => sp.GetRequiredService<AndroidPlatform>());
        services.AddSingleton<IAndroidLifecycle.IOnActivityRequestPermissionsResult>(sp => sp.GetRequiredService<AndroidPlatform>());
        services.AddSingleton<IAndroidLifecycle.IOnActivityResult>(sp => sp.GetRequiredService<AndroidPlatform>());

        services.AddSingleton<AndroidLifecycleExecutor>();
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<AndroidLifecycleExecutor>());
#elif IOS || MACCATALYST
        services.AddSingleton<IosPlatform>();
        services.AddSingleton<IPlatform>(sp => sp.GetRequiredService<IosPlatform>());

        services.AddSingleton<IosLifecycleExecutor>();
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<IosLifecycleExecutor>());
#elif MACOS
        services.AddSingleton<MacPlatform>();
        services.AddSingleton<IPlatform>(sp => sp.GetRequiredService<MacPlatform>());

        services.AddSingleton<MacLifecycleExecutor>();
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<MacLifecycleExecutor>());
#elif WINDOWS
        services.AddSingleton<WindowsPlatform>();
        services.AddSingleton<IPlatform>(sp => sp.GetRequiredService<WindowsPlatform>());
#endif
        return services;
    }
}
#endif
