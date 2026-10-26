using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.AgeSignals;

namespace Shiny;


public static class AgeSignalsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAgeSignals"/> - Apple Declared Age Range on iOS and macOS, Google Play Age Signals on
    /// Android, and an implementation that reports <see cref="AgeSignalStatus.NotSupported"/> everywhere else.
    /// </summary>
    /// <remarks>
    /// <para><b>Apple:</b> add the <c>com.apple.developer.declared-age-range</c> entitlement (and enable the capability
    /// for the App ID). The API answers on iOS / macOS 26+; earlier versions report NotSupported.</para>
    /// <para><b>Android:</b> install from Google Play (sideloaded builds fail with <see cref="AgeSignalsErrorReason.AppNotOwned"/>).
    /// Custom minimum ages are set in Play Console, not at runtime.</para>
    /// </remarks>
    public static IServiceCollection AddAgeSignals(this IServiceCollection services)
    {
#if ANDROID
        services.TryAddSingleton<IAgeSignals, AndroidAgeSignals>();
#elif IOS || MACOS
        services.TryAddSingleton<IAgeSignals, AppleAgeSignals>();
#else
        services.TryAddSingleton<IAgeSignals, NotSupportedAgeSignals>();
#endif
        return services;
    }
}
