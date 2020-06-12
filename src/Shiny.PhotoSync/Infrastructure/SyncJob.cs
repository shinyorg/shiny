using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;
using Shiny.Net.Http;
using Shiny.Notifications;


namespace Shiny.PhotoSync.Infrastructure
{
    public class SyncJob : IJob
    {
        readonly IPhotoGalleryScanner scanner;
        readonly IHttpTransferManager transfers;
        readonly INotificationManager notifications;
        readonly IPhotoSyncDelegate syncDelegate;
        readonly SyncConfig config;


        public SyncJob(IPhotoGalleryScanner scanner, 
                       IHttpTransferManager transfers,
                       INotificationManager notifications,
                       IPhotoSyncDelegate syncDelegate,
                       SyncConfig config)
        {
            this.scanner = scanner;
            this.transfers = transfers;
            this.notifications = notifications;
            this.syncDelegate = syncDelegate;
            this.config = config;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            var photos = await this.scanner.GetPhotosSince(DateTime.Now);
            if (photos?.Any() ?? false)
            { 
                foreach (var photo in photos)
                    await this.ProcessPhoto(photo);
            }
            return true;
        }


        async Task ProcessPhoto(Photo photo)
        {
            var result = await this.syncDelegate.CanSync(photo);
            if (!result)
                return;

            var headers = await this.syncDelegate.GetUploadHeaders(photo);
            await this.transfers.Enqueue(new HttpTransferRequest(
                this.config.UploadToUri,
                photo.FilePath,
                true
            )
            {
                Headers = headers,
                UseMeteredConnection = this.config.AllowUploadOnMeteredConnection
            });

            if (this.config.ShowBadgeCount)
                this.notifications.Badge = this.notifications.Badge + 1;
        }
    }
}
