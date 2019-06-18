using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Caching;
using Shiny.Infrastructure;
using Shiny.Settings;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseSqliteStorage(this IServiceCollection services)
        {
            services.AddIfNotRegistered<ShinySqliteConnection>();
            services.AddSingleton<IRepository, SqliteRepository>();
        }


        public static void UseSqliteCache(this IServiceCollection services)
        {
            services.AddIfNotRegistered<ShinySqliteConnection>();
            services.AddSingleton<ICache, SqliteCache>();
        }


        public static void UseSqliteSettings(this IServiceCollection services)
        {
            services.AddIfNotRegistered<ShinySqliteConnection>();
            services.AddSingleton<ISettings, SqliteSettings>();
        }
    }
}
