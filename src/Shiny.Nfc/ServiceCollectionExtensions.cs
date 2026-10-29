using Microsoft.Extensions.DependencyInjection;
using Shiny.Nfc;
using Shiny.Nfc.Infrastructure;

namespace Shiny;


public static class NfcServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="INfcManager"/> for reading and writing NDEF tags.
    /// </summary>
    /// <remarks>
    /// Platform backends (Android, iOS, PC/SC on Windows/macOS/Linux, Web NFC on Blazor) are not
    /// wired in yet - every platform currently gets the not-supported manager.
    /// </remarks>
    public static IServiceCollection AddNfc(this IServiceCollection services)
        => services.AddNotSupportedNfc();


    /// <summary>
    /// Registers an <see cref="INfcManager"/> that reports no NFC, for server, console and test hosts
    /// that resolve it from a shared library.
    /// </summary>
    public static IServiceCollection AddNotSupportedNfc(this IServiceCollection services)
    {
        services.AddSingleton<INfcManager, NotSupportedNfcManager>();
        return services;
    }
}
