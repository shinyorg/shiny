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
        readonly SyncConfig config;


        public SyncJob(IPhotoGalleryScanner scanner, 
                       IHttpTransferManager transfers,
                       INotificationManager notifications,
                       SyncConfig config)
        {
            this.scanner = scanner;
            this.transfers = transfers;
            this.notifications = notifications;
            this.config = config;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            var photos = await this.scanner.GetPhotosSince(DateTime.Now);
            if (photos?.Any() ?? false)
            { 
                foreach (var photo in photos)
                { 
                    await this.transfers.Enqueue(new HttpTransferRequest(
                        this.config.UploadToUri,
                        photo.FilePath, 
                        true
                    ));
                }

                if (this.config.ShowBadgeCount)
                    this.notifications.Badge = this.notifications.Badge + photos.Count();

                return true;
            }
            return false;
        }
    }
}
