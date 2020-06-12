using System;
using System.Threading.Tasks;


namespace Shiny.MediaSync
{
    public interface IMediaSyncManager
    {
        string DefaultUploadUri { get; set; }
        MediaTypes SyncTypes { get; set; }
        bool AllowUploadOnMeteredConnection { get; set; }
        bool ShowBadgeCount { get; set; }
        DateTimeOffset SyncFrom { get; set; }

        //int QueueCount { get; }
        Task<AccessState> RequestAccess();
        Task ClearQueue();
    }
}
