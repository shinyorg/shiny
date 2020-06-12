using System;
using System.Threading.Tasks;
using Shiny.Net.Http;
using Shiny.Notifications;


namespace Shiny.PhotoSync.Infrastructure
{
    public class PhotoSyncManagerImpl : NotifyPropertyChanged, IPhotoSyncManager
    {
        readonly IPhotoGalleryScanner scanner;
        readonly IHttpTransferManager transfers;


        public PhotoSyncManagerImpl(IPhotoGalleryScanner scanner,
                                    SyncConfig config,
                                    INotificationManager notifications,
                                    IHttpTransferManager transfers)
        { 
            this.scanner = scanner;
            this.transfers = transfers;
        }


        bool enabled = true;
        public bool IsEnabled 
        { 
            get => this.enabled; 
            set => this.Set(ref this.enabled, value);
        }


        public Task ClearQueue()
        {
            // TODO: only clear http transfers that are photo sync - how?  store ids?  tag them?
            throw new NotImplementedException();
        }


        public Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }
    }
}
