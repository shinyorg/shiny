using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny;


public static class AppStartupInstallerExtensions
{
    /// <summary>
    /// Registers <see cref="IAppStartupInstaller"/> for installing the app into the operating system's
    /// startup (launch at login) list on Windows, macOS and Mac Catalyst, and Linux - where it can also be
    /// a systemd unit (see <see cref="AppStartupOptions.LinuxMode"/>). Every call reports
    /// <see cref="AppStartupState.NotSupported"/> on Android, iOS and tvOS.
    /// </summary>
    /// <param name="configure">Optional configuration of <see cref="AppStartupOptions"/>.</param>
    public static IServiceCollection AddAppStartupInstaller(this IServiceCollection services, Action<AppStartupOptions>? configure = null)
    {
        var options = new AppStartupOptions();
        configure?.Invoke(options);

        // registered by factory rather than by type so nothing here needs reflection under AOT
        services.TryAddSingleton<IAppStartupInstaller>(_ => new AppStartupInstaller(options));
        return services;
    }
}
