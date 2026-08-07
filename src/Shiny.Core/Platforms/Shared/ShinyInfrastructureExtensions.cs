#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        // built explicitly - AndroidLifecycleExecutor is a Java.Lang.Object, so it must expose the
        // (IntPtr, JniHandleOwnership) JNI constructor.  Containers that pick a constructor by
        // "most resolvable arguments" have to weigh that overload and report a misleading
        // "unable to find constructor" when any real dependency below fails to resolve
        services.AddSingleton<AndroidLifecycleExecutor>(sp => new AndroidLifecycleExecutor(
            sp.GetRequiredService<ILogger<AndroidLifecycleExecutor>>(),
            sp.GetRequiredService<AndroidPlatform>(),
            sp.GetServices<IAndroidLifecycle.IApplicationLifecycle>(),
            sp.GetServices<IAndroidLifecycle.IOnActivityOnCreate>(),
            sp.GetServices<IAndroidLifecycle.IOnActivityRequestPermissionsResult>(),
            sp.GetServices<IAndroidLifecycle.IOnActivityNewIntent>(),
            sp.GetServices<IAndroidLifecycle.IOnActivityResult>()
        ));
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
