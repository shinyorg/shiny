using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Printing;
#if !PLATFORM
using Shiny.Printing.Blazor;
#endif

namespace Shiny;


public static class PrintingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the platform <see cref="IPrintService"/>: AirPrint (Apple), <c>PrintManager</c>
    /// (Android), GDI+ / shell (Windows), the browser print dialog on Blazor WebAssembly, or the CUPS
    /// <c>lp</c> backend (Linux and non-MAUI macOS).
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
        if (OperatingSystem.IsBrowser())
        {
            services.AddBlazorPrinting();
        }
        else
        {
            services.TryAddSingleton<ICupsProcessRunner, CupsProcessRunner>();
            services.TryAddSingleton<IPrintService, CupsPrintService>();
        }
#endif
        return services;
    }

#if !PLATFORM

    /// <summary>
    /// Registers the Blazor <see cref="IPrintService"/>, which prints via the browser dialog
    /// (<c>window.print()</c>). <see cref="AddNativePrinting"/> already does this on Blazor
    /// WebAssembly; call this directly from a Blazor Server app, whose code runs on the server but
    /// whose print dialog belongs to the browser. The script is served from
    /// <c>_content/Shiny.Printing/</c> (automatic for a referenced Razor class library).
    /// </summary>
    public static IServiceCollection AddBlazorPrinting(this IServiceCollection services)
    {
        services.TryAddScoped<IPrintService, BlazorPrintService>();
        return services;
    }
#endif
}
