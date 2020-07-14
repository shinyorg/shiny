using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseBlePrinters(this IServiceCollection services)
        {
#if NETSTANDARD2_0
            return false;
#else
            //services.AddSingleton<IPrinterManager, ZebraPrinterManager>();
            return true;
#endif
        }
    }
}
