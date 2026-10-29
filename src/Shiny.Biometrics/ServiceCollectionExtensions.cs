using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Biometrics;

namespace Shiny;


public static class BiometricsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IBiometrics"/> - LocalAuthentication on iOS, Mac Catalyst and macOS, AndroidX BiometricPrompt
    /// on Android, Windows Hello on Windows, and an implementation that reports
    /// <see cref="BiometricStatus.NotSupported"/> everywhere else.
    /// </summary>
    /// <remarks>
    /// <para><b>iOS:</b> add <c>NSFaceIDUsageDescription</c> to Info.plist - Face ID devices refuse to prompt without it.</para>
    /// <para><b>Android:</b> the prompt is shown from the current activity, which must be a <c>FragmentActivity</c>
    /// (MAUI's <c>MauiAppCompatActivity</c> is). The biometric permissions merge in from the AndroidX manifest.</para>
    /// </remarks>
    public static IServiceCollection AddBiometrics(this IServiceCollection services)
    {
#if ANDROID
        services.TryAddSingleton<IBiometrics, AndroidBiometrics>();
#elif APPLE
        services.TryAddSingleton<IBiometrics, AppleBiometrics>();
#elif WINDOWS
        services.TryAddSingleton<IBiometrics, WindowsBiometrics>();
#else
        services.TryAddSingleton<IBiometrics, NotSupportedBiometrics>();
#endif
        return services;
    }
}
