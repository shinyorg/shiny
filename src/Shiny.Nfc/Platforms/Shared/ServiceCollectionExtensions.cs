#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Shiny.Nfc;

namespace Shiny;


public static class ServiceCollectionExtensions
{

    public static INfcManager Nfc(this ShinyContainer container) => container.GetService<INfcManager>();


    /// <summary>
    /// Attempts to register NFC services with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddNfc(this IServiceCollection services)
        => services.AddShinyService<NfcManager>();
}
#endif
