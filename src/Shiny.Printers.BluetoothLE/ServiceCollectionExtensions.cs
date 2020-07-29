using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Printers;
using Shiny.Printers.BluetoothLE;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseBlePrinters(this IServiceCollection services, BlePrinterConfig config)
        {
            services.UseBleClient();
            services.AddSingleton(config);
            services.AddSingleton<IPrinterManager, BlePrinterManager>();
        }
    }
}
