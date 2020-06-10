using System;
using System.Threading.Tasks;


namespace Shiny.PhotoSync
{
    public interface IPhotoSyncManager
    {
        //bool ShowBadgeCount { get; set; }
        //int QueueCount { get; }
        Task ClearQueue();
    }


    public class PhotoSyncManager : IPhotoSyncManager
    {
        public Task ClearQueue()
        {
            // TODO: only clear http transfers that are photo sync - how?  store ids?  tag them?
            throw new NotImplementedException();
        }
    }
}
