using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.PhotoSync;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePhotoSync<TDelegate>(this IServiceCollection services, SyncConfig config)
            where TDelegate : class, IPhotoSyncDelegate
            => services.UsePhotoSync(config, typeof(TDelegate));


        public static bool UsePhotoSync(this IServiceCollection services, SyncConfig config, Type delegateType)
            where TDelegate : class, IPhotoSyncDelegate
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new PhotoSyncModule(config, delegateType));
            return true;
#endif
        }
    }
}