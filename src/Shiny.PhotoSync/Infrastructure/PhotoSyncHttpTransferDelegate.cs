using System;
using System.Threading.Tasks;
using Shiny.Net.Http;
using Shiny.Notifications;

namespace Shiny.PhotoSync.Infrastructure
{
    public class PhotoSyncHttpTransferDelegate : IHttpTransferDelegate
    {
        public PhotoSyncHttpTransferDelegate(INotificationManager notifications)
        {

        }


        public Task OnCompleted(HttpTransfer transfer)
        {
            return Task.CompletedTask;
        }


        public Task OnError(HttpTransfer transfer, Exception ex)
        {
            return Task.CompletedTask;
        }
    }
}
