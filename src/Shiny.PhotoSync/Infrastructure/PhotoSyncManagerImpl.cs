using System;
using System.Threading.Tasks;


namespace Shiny.PhotoSync.Infrastructure
{
    public class PhotoSyncManagerImpl : IPhotoSyncManager
    {
        public Task ClearQueue()
        {
            // TODO: only clear http transfers that are photo sync - how?  store ids?  tag them?
            throw new NotImplementedException();
        }
    }
}
