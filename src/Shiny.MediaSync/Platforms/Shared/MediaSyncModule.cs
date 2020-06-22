#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.MediaSync.Infrastructure;


namespace Shiny.MediaSync
{
    public class MediaSyncModule : ShinyModule
    {
        readonly Type delegateType;


        public MediaSyncModule(Type? delegateType)
        {
            this.delegateType = delegateType ?? typeof(MediaSyncDelegate);
        }


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(typeof(IMediaSyncDelegate), this.delegateType);
            services.AddSingleton<IMediaSyncManager, MediaSyncManagerImpl>();
            services.RegisterJob(
                typeof(SyncJob),
                runInForeground: true
            );
            services.UseNotifications(true);
            services.UseHttpTransfers<MediaSyncHttpTransferDelegate>();
            services.AddSingleton<IMediaGalleryScanner, MediaGalleryScannerImpl>();
        }
    }
}
#endif