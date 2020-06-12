using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Net.Http;
using Shiny.Notifications;


namespace Shiny.MediaSync.Infrastructure
{
    public class SyncJob : IJob
    {
        readonly IMediaSyncManager syncManager;
        readonly IMediaGalleryScanner scanner;
        readonly IHttpTransferManager transfers;
        readonly INotificationManager notifications;
        readonly IMediaSyncDelegate syncDelegate;
        readonly IRepository repository;
        const string LastSyncKey = nameof(LastSyncKey);


        public SyncJob(IMediaSyncManager syncManager,
                       IMediaGalleryScanner scanner, 
                       IHttpTransferManager transfers,
                       INotificationManager notifications,
                       IMediaSyncDelegate syncDelegate,
                       IRepository repository)
        {
            this.syncManager = syncManager;
            this.scanner = scanner;
            this.transfers = transfers;
            this.notifications = notifications;
            this.syncDelegate = syncDelegate;
            this.repository = repository;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            // TODO: verify gallery access
            var lastSync = jobInfo.GetParameter(LastSyncKey, DateTimeOffset.MinValue);
            var items = await this.scanner.GetMediaSince(lastSync);
            if (items?.Any() ?? false)
            {                 
                foreach (var item in items)
                {
                    var sync = await this.CanProcess(item);
                    if (sync)
                        await this.Process(item);
                }
            }
            jobInfo.SetParameter(LastSyncKey, DateTimeOffset.UtcNow);
            return true;
        }


        async Task<bool> CanProcess(Media media)
        {            
            if (!this.syncManager.IsVideoSyncEnabled && media.IsVideo)
                return false;

            if (!this.syncManager.IsPhotoSyncEnabled && !media.IsVideo)
                return false;

            // TODO: ensure it isn't in queue already?  last job swap may not have finished
            //await this.repository.Get<SyncItem>(media)
            var result = await this.syncDelegate.CanSync(media);
            if (!result)
                return false;

            return true;
        }

        async Task Process(Media media)
        {
            var suggestedRequest = new HttpTransferRequest(
                this.syncManager.DefaultUploadUri,
                media.FilePath,
                true
            );
            suggestedRequest.HttpMethod = HttpMethod.Get;
            suggestedRequest.UseMeteredConnection = this.syncManager.AllowUploadOnMeteredConnection;
            var request = await this.syncDelegate.PreRequest(suggestedRequest, media);

            var transfer = await this.transfers.Enqueue(request);
            await this.repository.Set(
                transfer.Identifier,
                new SyncItem
                {
                    Id = transfer.Identifier,
                    FilePath = media.FilePath,
                    DateStarted = DateTimeOffset.UtcNow
                }
            );

            if (this.syncManager.ShowBadgeCount)
                this.notifications.Badge = this.notifications.Badge + 1;
        }
    }
}
