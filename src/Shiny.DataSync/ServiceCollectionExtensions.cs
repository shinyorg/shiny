using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.DataSync;
using Shiny.DataSync.Infrastructure;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseDataSync<TDelegate>(this IServiceCollection services, bool syncOnAnyConnection) where TDelegate : class, IDataSyncDelegate
        {
            services.AddSingleton<IDataSyncManager, DataSyncManager>();
            services.AddSingleton<IDataSyncDelegate, TDelegate>();
            services.UseJobForegroundService(TimeSpan.FromSeconds(30));

            services.RegisterJob(
                typeof(SyncJob),
                requiredNetwork: syncOnAnyConnection
                    ? Jobs.InternetAccess.Any
                    : Jobs.InternetAccess.Unmetered,
                runInForeground: true
            );
        }
    }
}
