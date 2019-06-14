using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Caching;
using Shiny.Infrastructure;
using Shiny.Settings;

namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseLiteDbStorage(this IServiceCollection services)
        {
            services.AddIfNotRegistered<ShinyLiteDatabase>();
            //services.AddSingleton<IRepository, SqliteRepository>();
        }


        public static void UseLiteDbCache(this IServiceCollection services)
        {
            services.AddIfNotRegistered<ShinyLiteDatabase>();
            services.AddSingleton<ICache, LiteDbCache>();
        }


        public static void UseLiteDbSettings(this IServiceCollection services)
        {
            services.AddIfNotRegistered<ShinyLiteDatabase>();
            services.AddSingleton<ISettings, LiteDbSettings>();
        }
    }
}
