using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Printing;
using Shiny.Printing.Blazor;

namespace Shiny;


public static class BlazorPrintingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Blazor <see cref="IPrintService"/>, which prints via the browser dialog
    /// (<c>window.print()</c>). Requires the host to serve this package's static web assets
    /// (automatic for referenced Razor class libraries).
    /// </summary>
    public static IServiceCollection AddBlazorPrinting(this IServiceCollection services)
    {
        services.TryAddScoped<IPrintService, BlazorPrintService>();
        return services;
    }
}
