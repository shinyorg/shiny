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
            if (!this.syncManager.IsEnabled)
                return false;

            // TODO: verify photo access
            var lastSync = jobInfo.GetParameter(LastSyncKey, DateTimeOffset.MinValue);
            var photos = await this.scanner.GetMediaSince(lastSync);
            if (photos?.Any() ?? false)
            { 
                // TODO: ensure it isn't in queue already?  last job swap may not have finished
                foreach (var photo in photos)
                    await this.ProcessPhoto(photo);
            }
            jobInfo.SetParameter(LastSyncKey, DateTimeOffset.UtcNow);
            return true;
        }


        async Task ProcessPhoto(Media photo)
        {
            var result = await this.syncDelegate.CanSync(photo);
            if (!result)
                return;

            var suggestedRequest = new HttpTransferRequest(
                this.syncManager.DefaultUploadUri,
                photo.FilePath,
                true
            );
            suggestedRequest.HttpMethod = HttpMethod.Get;
            suggestedRequest.UseMeteredConnection = this.syncManager.AllowUploadOnMeteredConnection;
            var request = await this.syncDelegate.PreRequest(suggestedRequest, photo);

            var transfer = await this.transfers.Enqueue(request);
            await this.repository.Set(
                transfer.Identifier,
                new SyncItem
                {
                    Id = transfer.Identifier,
                    FilePath = photo.FilePath,
                    DateStarted = DateTimeOffset.UtcNow
                }
            );

            if (this.syncManager.ShowBadgeCount)
                this.notifications.Badge = this.notifications.Badge + 1;
        }
    }
}
