using System;
using System.Threading.Tasks;


namespace Shiny.PhotoSync
{
    public interface IPhotoSyncManager
    {
        bool IsEnabled { get; set; }
        //bool ShowBadgeCount { get; set; }
        //int QueueCount { get; }
        Task<AccessState> RequestAccess();
        Task ClearQueue();
    }
}
