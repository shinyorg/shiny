using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseJsonNetSerialization(this IServiceCollection services)
            => services.AddSingleton<Shiny.Infrastructure.ISerializer, Shiny.Integrations.JsonNet.JsonNetSerializer>();
    }
}
