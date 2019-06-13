using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Caching;
using Shiny.Infrastructure;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseSqliteStorage(this IServiceCollection services)
        {
            //services.AddIfNotRegister<ShinySqliteConnection>();
            services.AddSingleton<IRepository, SqliteRepository>();
        }


        public static void UseSqliteCache(this IServiceCollection services)
        {
            //services.AddIfNotRegister<ShinySqliteConnection>();
            services.AddSingleton<ICache, SqliteCache>();
        }
    }
}
