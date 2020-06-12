using System;
using System.Threading.Tasks;
using Shiny.Net.Http;


namespace Shiny.MediaSync
{
    public interface IMediaSyncDelegate
    {
        Task<bool> CanSync(MediaAsset media);
        Task<HttpTransferRequest> PreRequest(HttpTransferRequest request, MediaAsset media);
        Task OnSyncCompleted(MediaAsset media);
        // TODO: return true to requeue?
        //Task<bool> OnSyncError(Media media, Exception exception);
    }


    public class MediaSyncDelegate : IMediaSyncDelegate
    {
        public virtual Task<bool> CanSync(MediaAsset media) => Task.FromResult(true);
        public virtual Task OnSyncCompleted(MediaAsset media) => Task.CompletedTask;
        public virtual Task<HttpTransferRequest> PreRequest(HttpTransferRequest request, MediaAsset media)
            => Task.FromResult(request);
    }
}
