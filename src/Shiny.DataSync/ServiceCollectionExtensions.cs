using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.DataSync;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseDataSync<TDelegate>(this IServiceCollection services) where TDelegate : class, IDataSyncDelegate
        {
            //services.AddSingleton<ISyncManager, S>
            services.AddSingleton<IDataSyncDelegate, TDelegate>();
            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
        }
    }
}
