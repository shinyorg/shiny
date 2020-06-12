using System;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net.Http;
using Shiny.Notifications;


namespace Shiny.MediaSync.Infrastructure
{
    public class MediaSyncManagerImpl : NotifyPropertyChanged, IMediaSyncManager
    {
        readonly IMediaGalleryScanner scanner;
        readonly IHttpTransferManager transferManager;
        readonly INotificationManager notificationManager;
        readonly IRepository repository;


        public MediaSyncManagerImpl(IMediaGalleryScanner scanner,
                                    INotificationManager notificationManager,
                                    IHttpTransferManager transferManager,
                                    IRepository repository)
        { 
            this.scanner = scanner;
            this.transferManager = transferManager;
            this.notificationManager = notificationManager;
            this.repository = repository;
        }


        MediaTypes syncTypes = MediaTypes.Audio | MediaTypes.Image | MediaTypes.Video;
        public MediaTypes SyncTypes
        {
            get => this.syncTypes;
            set => this.Set(ref this.syncTypes, value);
        }


        bool allowUploadOnMeteredConnection = false;
        public bool AllowUploadOnMeteredConnection 
        { 
            get => this.allowUploadOnMeteredConnection;
            set => this.Set(ref this.allowUploadOnMeteredConnection, value);
        }


        // TODO: if disabling - clear badges for sync items
        bool showBadgeCount = true;
        public bool ShowBadgeCount 
        { 
            get => this.showBadgeCount; 
            set => this.Set(ref this.showBadgeCount, value); 
        }


        string defaultUploadUri;
        public string DefaultUploadUri
        {
            get => this.defaultUploadUri;
            set => this.Set(ref this.defaultUploadUri, value);
        }


        DateTimeOffset syncFrom;
        public DateTimeOffset SyncFrom
        {
            get => this.syncFrom;
            set => this.Set(ref this.syncFrom, value);
        }


        public async Task ClearQueue()
        {
            var items = await this.repository.GetAll<SyncItem>();
            if (items.Count == 0)
                return;

            var ids = items.Select(x => x.Id).ToList();
            var filter = new QueryFilter
            {
                Ids = ids,
                Direction = DirectionFilter.Upload
            };
            await this.transferManager.Cancel(filter);
            if (this.ShowBadgeCount)
                this.notificationManager.Badge = this.notificationManager.Badge - ids.Count;
        }


        public Task<AccessState> RequestAccess() => this.scanner.RequestAccess();
    }
}
