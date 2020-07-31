using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.DataSync
{
    public static class ServiceCollectionExtensions
    {
        public static void UseDataSync<TDelegate>(this IServiceCollection services) where TDelegate : IDataSyncDelegate
        {

        }
    }
}
