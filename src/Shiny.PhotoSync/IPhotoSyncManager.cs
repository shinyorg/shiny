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
}
