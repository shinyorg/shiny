using System;
using System.Threading.Tasks;
using Shiny.Net.Http;


namespace Shiny.MediaSync
{
    public interface IMediaSyncDelegate
    {
        Task<bool> CanSync(Media media);
        Task<HttpTransferRequest> PreRequest(HttpTransferRequest request, Media media);
        Task OnSyncCompleted(Media media);
        // TODO: return true to requeue?
        //Task<bool> OnSyncError(Media media, Exception exception);
    }


    public class MediaSyncDelegate : IMediaSyncDelegate
    {
        public virtual Task<bool> CanSync(Media media) => Task.FromResult(true);
        public virtual Task OnSyncCompleted(Media media) => Task.CompletedTask;
        public virtual Task<HttpTransferRequest> PreRequest(HttpTransferRequest request, Media media)
            => Task.FromResult(request);
    }
}
