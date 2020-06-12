using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.MediaSync;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseMediaSync<TDelegate>(this IServiceCollection services, SyncConfig config)
            where TDelegate : class, IMediaSyncDelegate
            => services.UseMediaSync(config, typeof(TDelegate));


        public static bool UseMediaSync(this IServiceCollection services, SyncConfig config, Type? delegateType = null)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new MediaSyncModule(config, delegateType));
            return true;
#endif
        }
    }
}