using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.MediaSync.Infrastructure
{
    public interface IMediaGalleryScanner
    {
        Task<AccessState> RequestAccess();
        Task<IEnumerable<MediaAsset>> Query(MediaTypes mediaTypes, DateTimeOffset date);
    }
}
