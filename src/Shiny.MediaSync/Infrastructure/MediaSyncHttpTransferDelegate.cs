using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net.Http;
using Shiny.Notifications;


namespace Shiny.MediaSync.Infrastructure
{
    public class MediaSyncHttpTransferDelegate : IHttpTransferDelegate
    {
        readonly IMediaSyncManager syncManager;
        readonly INotificationManager notificationManager;
        readonly IDataService dataService;


        public MediaSyncHttpTransferDelegate(IMediaSyncManager syncManager,
                                             INotificationManager notificationManager,
                                             IDataService dataService)
        {
            this.syncManager = syncManager;
            this.notificationManager = notificationManager;
            this.dataService = dataService;
        }


        public Task OnCompleted(HttpTransfer transfer)
            => this.TryUpdate(transfer);


        public Task OnError(HttpTransfer transfer, Exception ex)
        {
            // TODO: file error with delegate?
            return this.TryUpdate(transfer);
        }


        async Task TryUpdate(HttpTransfer transfer)
        {
            var result = await this.dataService.Remove(transfer.Identifier);
            if (result && this.syncManager.ShowBadgeCount)
                this.notificationManager.Badge = this.notificationManager.Badge - 1;
        }
    }
}
