using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.PhotoSync.Infrastructure;


namespace Shiny.PhotoSync
{
    public class PhotoSyncModule : ShinyModule
    {
        readonly SyncConfig config;
        readonly Type delegateType;


        public PhotoSyncModule(SyncConfig config, Type delegateType)
        {
            this.config = config;
            this.delegateType = delegateType;
        }


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(typeof(IPhotoSyncDelegate), this.delegateType);
            services.AddSingleton(config);
            services.AddSingleton<IPhotoSyncManager, PhotoSyncManagerImpl>();
            services.RegisterJob(
                typeof(SyncJob),
                runInForeground: true
            );
            services.UseNotifications(true);
            services.UseHttpTransfers<PhotoSyncHttpTransferDelegate>();
            services.AddSingleton<IPhotoGalleryScanner, PhotoGalleryScannerImpl>();
        }


        public override void OnContainerReady(IServiceProvider services)
        {
            // TODO: this should request access to photo gallery right away
            base.OnContainerReady(services);
        }
    }
}
