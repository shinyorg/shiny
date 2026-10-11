using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.AppStore;

namespace Shiny;


public static class AppStoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAppStore"/> for the store the app ships through: the Apple App Store (iOS,
    /// Mac Catalyst) or Mac App Store (macOS), Google Play, the Microsoft Store, or Flatpak/Snap on Linux.
    /// Plain .NET anywhere else gets a store that finds nothing and opens nothing.
    /// </summary>
    /// <param name="configure">Store identifiers - see <see cref="AppStoreOptions"/>. Windows needs
    /// <see cref="AppStoreOptions.WindowsProductId"/>; the others are detected from the running app.</param>
    public static IServiceCollection AddAppStore(this IServiceCollection services, Action<AppStoreOptions> configure)
    {
        var options = new AppStoreOptions();
        configure.Invoke(options);

        // registered by factory rather than by type so nothing here needs reflection under AOT
#if ANDROID
        services.TryAddSingleton<IAppStore>(sp => new AndroidAppStore(sp.GetRequiredService<AndroidPlatform>(), options));
#elif IOS || MACCATALYST || MACOS
        services.TryAddSingleton<IAppStore>(sp => new AppleAppStore(sp.GetRequiredService<IPlatform>(), options));
#elif WINDOWS
        services.TryAddSingleton<IAppStore>(sp => new WindowsAppStore(sp.GetRequiredService<IPlatform>(), options));
#else
        if (OperatingSystem.IsLinux())
            services.TryAddSingleton<IAppStore>(sp => new LinuxAppStore(sp.GetRequiredService<IPlatform>(), options));
        else
            services.TryAddSingleton<IAppStore, NotSupportedAppStore>();
#endif
        return services;
    }


    /// <summary>
    /// Registers <see cref="IAppStore"/> with the most common store identifiers passed directly - call
    /// it with no arguments when everything can be detected from the running app (all but Windows).
    /// </summary>
    public static IServiceCollection AddAppStore(
        this IServiceCollection services,
        string? appleAppId = null,
        string? androidPackageName = null,
        string? windowsProductId = null,
        string? linuxAppId = null,
        string? countryCode = null
    ) => services.AddAppStore(opts =>
    {
        if (appleAppId != null) opts.AppleAppId = appleAppId;
        if (androidPackageName != null) opts.AndroidPackageName = androidPackageName;
        if (windowsProductId != null) opts.WindowsProductId = windowsProductId;
        if (linuxAppId != null) opts.LinuxAppId = linuxAppId;
        if (countryCode != null) opts.CountryCode = countryCode;
    });
}
