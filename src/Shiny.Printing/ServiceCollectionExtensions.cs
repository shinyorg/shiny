using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Printing;

namespace Shiny;


public static class PrintingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the platform <see cref="IPrintService"/>: AirPrint (Apple), <c>PrintManager</c>
    /// (Android), GDI+ / shell (Windows), or the CUPS <c>lp</c> backend (Linux and non-MAUI macOS).
    /// For Blazor WebAssembly use <c>AddBlazorPrinting()</c> from <c>Shiny.Printing.Blazor</c> instead.
    /// </summary>
    public static IServiceCollection AddNativePrinting(this IServiceCollection services)
    {
#if ANDROID
        services.TryAddSingleton<IPrintService, AndroidPrintService>();
#elif IOS || MACCATALYST
        services.TryAddSingleton<IPrintService, ApplePrintService>();
#elif WINDOWS
        services.TryAddSingleton<IPrintService, WindowsPrintService>();
#else
        services.TryAddSingleton<ICupsProcessRunner, CupsProcessRunner>();
        services.TryAddSingleton<IPrintService, CupsPrintService>();
#endif
        return services;
    }
}
