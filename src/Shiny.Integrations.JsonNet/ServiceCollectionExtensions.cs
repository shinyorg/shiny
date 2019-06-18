using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseJsonNetSerialization(this IServiceCollection services)
            => services.AddOrReplace<ISerializer, JsonNetSerializer>();
    }
}
