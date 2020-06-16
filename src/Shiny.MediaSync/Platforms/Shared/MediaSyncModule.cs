#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.MediaSync.Infrastructure;
using Shiny.MediaSync.Infrastructure.Sqlite;


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
            services.AddSingleton<IDataService, SqliteDataService>();
            services.AddSingleton<ShinySqliteConnection>();

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