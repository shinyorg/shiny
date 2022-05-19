using Microsoft.Extensions.DependencyInjection;
using Shiny.Nfc;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Attempts to register NFC services with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static bool UseNfc(this IServiceCollection services)
    {
#if IOS || MACCATALYST || ANDROID
        services.AddShinyServiceWithLifecycle<INfcManager, NfcManager>();
        return true;
#else
        return false;
#endif
    }
}
