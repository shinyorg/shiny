using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Printers;
using Shiny.Printers.Zebra;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseZebraPrinters(this IServiceCollection services)
            => services.AddSingleton<IPrinterManager, ZebraPrinterManager>();
    }
}
