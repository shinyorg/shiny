#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.MediaSync.Infrastructure;


namespace Shiny.MediaSync
{
    public class MediaSyncModule : ShinyModule
    {
        readonly SyncConfig config;
        readonly Type delegateType;


        public MediaSyncModule(SyncConfig config, Type? delegateType)
        {
            this.config = config;
            this.delegateType = delegateType ?? typeof(PhotoSyncDelegate);
        }


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(typeof(IMediaSyncDelegate), this.delegateType);
            services.AddSingleton(config);
            services.AddSingleton<IMediaSyncManager, MediaSyncManagerImpl>();
            services.RegisterJob(
                typeof(SyncJob),
                runInForeground: true
            );
            services.UseNotifications(true);
            services.UseHttpTransfers<MediaSyncHttpTransferDelegate>();
            services.AddSingleton<IMediaGalleryScanner, PhotoGalleryScannerImpl>();
        }


        public override void OnContainerReady(IServiceProvider services)
        {
            // TODO: this should request access to photo gallery right away
            base.OnContainerReady(services);
        }
    }
}
#endif