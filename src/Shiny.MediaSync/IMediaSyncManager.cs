using System;
using System.Threading.Tasks;


namespace Shiny.MediaSync
{
    public interface IMediaSyncManager
    {
        string DefaultUploadUri { get; set; }
        bool IsVideoSyncEnabled { get; set; }
        bool IsPhotoSyncEnabled { get; set; }
        bool AllowUploadOnMeteredConnection { get; set; }
        bool ShowBadgeCount { get; set; }

        //int QueueCount { get; }
        Task<AccessState> RequestAccess();
        Task ClearQueue();
    }
}
