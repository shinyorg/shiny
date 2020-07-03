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
        readonly IDataService dataService;


        public SyncJob(IMediaSyncManager syncManager,
                       IMediaGalleryScanner scanner, 
                       IHttpTransferManager transfers,
                       INotificationManager notifications,
                       IMediaSyncDelegate syncDelegate,
                       IDataService dataService)
        {
            this.syncManager = syncManager;
            this.scanner = scanner;
            this.transfers = transfers;
            this.notifications = notifications;
            this.syncDelegate = syncDelegate;
            this.dataService = dataService;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            if (this.syncManager.SyncTypes == null)
                return false;

            // TODO: verify gallery access
            var items = await this.scanner.Query(
                this.syncManager.SyncTypes.Value, 
                this.syncManager.SyncFrom
            );
            if (items?.Any() ?? false)
            {                 
                foreach (var item in items)
                {
                    var sync = await this.CanProcess(item);
                    if (sync)
                        await this.Process(item);
                }
            }
            this.syncManager.SyncFrom = DateTimeOffset.UtcNow;
            return true;
        }


        async Task<bool> CanProcess(MediaAsset media)
        {
            if (!this.syncManager.SyncTypes?.HasFlag(media.Type) ?? true)
                return false;

            // ensure it isn't in queue already?  last job swap may not have finished
            var item = await this.dataService.GetById(media.Identifier);
            if (item != null && this.transfers.GetTransfer(item.HttpTransferId) != null)
                return false;

            var result = await this.syncDelegate.CanSync(media);
            if (!result)
                return false;

            return true;
        }

        async Task Process(MediaAsset media)
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
            await this.dataService.Create(new SyncItem
            {
                Id = transfer.Identifier,
                FilePath = media.FilePath,
                DateStarted = DateTimeOffset.UtcNow
            });

            if (this.syncManager.ShowBadgeCount)
                this.notifications.Badge = this.notifications.Badge + 1;
        }
    }
}
