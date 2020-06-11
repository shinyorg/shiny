using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.PhotoSync;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePhotoSync(this IServiceCollection services, SyncConfig config)
        {
            services.AddSingleton(config);
            services.AddSingleton<IPhotoSyncManager, PhotoSyncManager>();
            services.RegisterJob(typeof(SyncJob)); 
#if NETSTANDARD
            return false;
#else
            services.AddSingleton<IPhotoGalleryScanner, PhotoGalleryScannerImpl>();
            return true;
#endif
        }
    }
}