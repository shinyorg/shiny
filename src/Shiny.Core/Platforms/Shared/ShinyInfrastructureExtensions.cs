#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Hosting;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny.Infrastructure;


public static class ShinyInfrastructureExtensions
{
    /// <summary>
    /// This is called by Shiny hosting - You should NOT be calling this yourself
    /// </summary>
    public static IServiceCollection AddShinyCoreServices(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();

#if ANDROID
        services.AddSingleton<AndroidPlatform>();
        services.AddSingleton<IPlatform>(sp => sp.GetRequiredService<AndroidPlatform>());
        services.AddSingleton<IAndroidLifecycle.IOnActivityRequestPermissionsResult>(sp => sp.GetRequiredService<AndroidPlatform>());
        services.AddSingleton<IAndroidLifecycle.IOnActivityResult>(sp => sp.GetRequiredService<AndroidPlatform>());

        services.AddSingleton<AndroidLifecycleExecutor>();
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<AndroidLifecycleExecutor>());

        services.AddKeyedSingleton<IKeyValueStore, SettingsKeyValueStore>(StoreKeys.Default);
        services.AddKeyedSingleton<IKeyValueStore, SecureKeyValueStore>(StoreKeys.Secure);
#elif IOS || MACCATALYST
        services.AddSingleton<IosPlatform>();
        services.AddSingleton<IPlatform>(sp => sp.GetRequiredService<IosPlatform>());

        services.AddSingleton<IosLifecycleExecutor>();
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<IosLifecycleExecutor>());

        services.AddKeyedSingleton<IKeyValueStore, SettingsKeyValueStore>(StoreKeys.Default);
        services.AddKeyedSingleton<IKeyValueStore, SecureKeyValueStore>(StoreKeys.Secure);
#elif MACOS
        services.AddSingleton<MacPlatform>();
        services.AddSingleton<IPlatform>(sp => sp.GetRequiredService<MacPlatform>());

        services.AddSingleton<MacLifecycleExecutor>();
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<MacLifecycleExecutor>());

        services.AddKeyedSingleton<IKeyValueStore, SettingsKeyValueStore>(StoreKeys.Default);
        services.AddKeyedSingleton<IKeyValueStore, SecureKeyValueStore>(StoreKeys.Secure);
#elif WINDOWS
        services.AddSingleton<WindowsPlatform>();
        services.AddSingleton<IPlatform>(sp => sp.GetRequiredService<WindowsPlatform>());

        services.AddKeyedSingleton<IKeyValueStore, SettingsKeyValueStore>(StoreKeys.Default);
        services.AddKeyedSingleton<IKeyValueStore, SecureKeyValueStore>(StoreKeys.Secure);
#endif
        // Unkeyed default — inject plain IKeyValueStore to get the "settings" store
        services.TryAddSingleton<IKeyValueStore>(
            sp => sp.GetRequiredKeyedService<IKeyValueStore>(StoreKeys.Default)
        );

        return services;
    }
}
#endif
