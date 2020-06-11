using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.PhotoSync;
using Shiny.PhotoSync.Infrastructure;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePhotoSync(this IServiceCollection services, SyncConfig config)
        {
#if NETSTANDARD
            return false;
#else
            // TODO: this should request access to photo gallery right away
            services.AddSingleton(config);
            services.AddSingleton<IPhotoSyncManager, PhotoSyncManagerImpl>();
            services.RegisterJob(typeof(SyncJob));
            services.UseNotifications(true);
            services.UseHttpTransfers<PhotoSyncHttpTransferDelegate>();
            services.AddSingleton<IPhotoGalleryScanner, PhotoGalleryScannerImpl>();
            return true;
#endif
        }
    }
}