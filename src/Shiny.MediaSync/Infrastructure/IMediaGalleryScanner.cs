using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.MediaSync.Infrastructure
{
    public interface IMediaGalleryScanner
    {
        Task<AccessState> RequestAccess();
        Task<IEnumerable<Media>> GetMediaSince(DateTimeOffset date);
    }
}
