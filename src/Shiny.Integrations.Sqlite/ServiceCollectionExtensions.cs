using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;
using Shiny.Integrations.Sqlite;
using Shiny.Integrations.Sqlite.Logging;
using Shiny.Stores;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="logLevel"></param>
        public static void AddSqliteLogging(this ILoggingBuilder builder, LogLevel logLevel = LogLevel.Warning)
        {
            builder.Services.TryAddSingleton<ShinySqliteConnection>();
            builder.AddProvider(new SqliteLoggerProvider(logLevel));
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="services"></param>
        public static void UseSqliteStorage(this IServiceCollection services)
        {
            services.TryAddSingleton<ShinySqliteConnection>();
            services.AddSingleton<IRepository, SqliteRepository>();
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="services"></param>
        public static void UseSqliteStore(this IServiceCollection services)
        {
            services.TryAddSingleton<ShinySqliteConnection>();
            services.AddSingleton<IKeyValueStore, SqliteKeyValueStore>();
        }
    }
}
