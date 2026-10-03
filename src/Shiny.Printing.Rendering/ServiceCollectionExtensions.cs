using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Printing.Rendering;

namespace Shiny;


public static class RenderingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IPrintDocumentRenderer"/> (SkiaSharp) so a thermal <c>PrintDocument</c> can
    /// be rendered to a PDF and printed through <c>IPrintService</c>.
    /// </summary>
    public static IServiceCollection AddPrintDocumentRendering(this IServiceCollection services)
    {
        services.TryAddSingleton<IPrintDocumentRenderer, SkiaPrintDocumentRenderer>();
        return services;
    }
}
