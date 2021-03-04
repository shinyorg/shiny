using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;
using Shiny.Integrations.Sqlite;
using Shiny.Stores;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// WARNING: this will not catch startup issues as the connection isn't ready until after startup - it will catch all delegates though
        /// </summary>
        /// <param name="services"></param>
        /// <param name="enableCrashes"></param>
        /// <param name="enableEvents"></param>
        public static void UseSqliteLogging(this ILoggingBuilder builder)
        {
            builder.Services.TryAddSingleton<ShinySqliteConnection>();
            //services.RegisterPostBuildAction(sp =>
            //{
            //    var conn = sp.GetService<ShinySqliteConnection>();
            //    var serializer = sp.GetService<ISerializer>();
            //    //Log.AddLogger(new SqliteLog(conn, serializer), enableCrashes, enableEvents);
            //});
        }


        public static void UseSqliteStorage(this IServiceCollection services)
        {
            services.TryAddSingleton<ShinySqliteConnection>();
            services.AddSingleton<IRepository, SqliteRepository>();
        }


        public static void UseSqliteStore(this IServiceCollection services)
        {
            services.TryAddSingleton<ShinySqliteConnection>();
            services.AddSingleton<IKeyValueStore, SqliteKeyValueStore>();
        }
    }
}
