using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseLiteDbStorage(this IServiceCollection services)
        {
            //services.AddIfNotRegister<ShinySqliteConnection>();
            //services.AddSingleton<IRepository, SqliteRepository>();
        }


        public static void UseLiteDbCache(this IServiceCollection services)
        {
            //services.AddIfNotRegister<ShinySqliteConnection>();
            //services.AddSingleton<ICache, SqliteCache>();
        }


        public static void UseLiteDbSettings(this IServiceCollection services)
        {
            //services.AddIfNotRegister<ShinySqliteConnection>();
            //services.AddSingleton<ISettings, SqliteSettings>();
        }
    }
}
