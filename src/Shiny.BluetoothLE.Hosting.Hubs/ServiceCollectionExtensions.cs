using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE.Hosting.Hubs.Infrastructure;


namespace Shiny.BluetoothLE.Hosting.Hubs
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterBleHub<THub>(this IServiceCollection services) where THub : IShinyBleHub
        {
            HostingHubModule.HubTypes.Add(typeof(THub));
            services.RegisterModule(new HostingHubModule());
        }
    }
}
