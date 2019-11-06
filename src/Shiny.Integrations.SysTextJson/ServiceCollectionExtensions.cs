using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Integrations.SysTextJson;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseSystemTextJsonSerialization(this IServiceCollection services)
            => services.AddSingleton<ISerializer, SysTextJsonSerializer>();
    }
}
