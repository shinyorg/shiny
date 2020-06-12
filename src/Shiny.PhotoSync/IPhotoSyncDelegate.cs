using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.PhotoSync
{
    public interface IPhotoSyncDelegate
    {
        Task<bool> CanSync(Photo photo);
        Task<IDictionary<string, string>> GetUploadHeaders(Photo photo);
        Task OnPhotoSync(Photo photo);
    }
}
