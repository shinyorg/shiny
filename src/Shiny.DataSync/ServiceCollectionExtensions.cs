using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.DataSync;
using Shiny.DataSync.Infrastructure;
using Shiny.Jobs;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseDataSync<TDelegate>(this IServiceCollection services, bool syncOnAnyConnection = true) where TDelegate : class, IDataSyncDelegate
        {
            services.AddSingleton<IDataSyncManager, DataSyncManager>();
            services.AddSingleton<IDataSyncDelegate, TDelegate>();
            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.RegisterJob(new JobInfo(typeof(SyncJob), SyncJob.JobName)
            {
                RequiredInternetAccess = syncOnAnyConnection
                    ? Jobs.InternetAccess.Any
                    : Jobs.InternetAccess.Unmetered,
                RunOnForeground = true,
                IsSystemJob = true,
                BatteryNotLow = true
            });
        }
    }
}
