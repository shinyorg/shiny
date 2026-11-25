using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Locations;

namespace Shiny;


/// <summary>Registration for the GeoClue2 GPS manager.</summary>
public static class LinuxGpsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IGpsManager"/> for Linux, backed by GeoClue2 over the D-Bus system bus. Does
    /// nothing on other operating systems, so it can sit beside <c>AddGps()</c> in shared startup code.
    /// </summary>
    /// <remarks>
    /// Needs GeoClue (installed by default on GNOME, KDE and most desktop distributions) and a desktop id the
    /// location agent can show - set <see cref="LinuxGpsOptions.DesktopId"/> to the app's <c>.desktop</c> file
    /// name. Replaces any <see cref="IGpsManager"/> registered before it.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional GeoClue settings.</param>
    public static IServiceCollection AddLinuxGps(this IServiceCollection services, Action<LinuxGpsOptions>? configure = null)
    {
        if (!OperatingSystem.IsLinux())
            return services;

        var options = new LinuxGpsOptions();
        configure?.Invoke(options);

        // last registration wins for IGpsManager, but a stale one would still run as a startup task - drop it
        services.RemoveAll<IGpsManager>();
        services.AddSingleton(options);
        services.AddSingleton<LinuxGpsManager>();
        services.AddSingleton<IGpsManager>(sp => sp.GetRequiredService<LinuxGpsManager>());
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<LinuxGpsManager>());
        return services;
    }


    /// <summary>
    /// Registers <see cref="IGpsManager"/> for Linux with an <see cref="IGpsDelegate"/> that receives every reading.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional GeoClue settings.</param>
    public static IServiceCollection AddLinuxGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services, Action<LinuxGpsOptions>? configure = null)
        where TDelegate : class, IGpsDelegate
    {
        if (!OperatingSystem.IsLinux())
            return services;

        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        return services.AddLinuxGps(configure);
    }
}
