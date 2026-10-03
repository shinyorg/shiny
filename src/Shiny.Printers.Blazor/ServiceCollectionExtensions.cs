using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Printers.Blazor;

namespace Shiny;


public static class BrowserPrintersServiceCollectionExtensions
{
    /// <summary>
    /// Registers browser-transport thermal printing: the <see cref="BrowserPrinterManager"/>, scoped so
    /// each Blazor Server circuit gets its own JS module reference. Requires the host to serve this
    /// package's static web assets (automatic for referenced Razor class libraries).
    /// </summary>
    public static IServiceCollection AddBrowserPrinting(this IServiceCollection services)
    {
        services.TryAddScoped<BrowserPrinterManager>();
        return services;
    }
}
