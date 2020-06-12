using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.MediaSync.Infrastructure
{
    public class PhotoGalleryScannerImpl : IMediaGalleryScanner
    {
        public Task<IEnumerable<Media>> GetMediaSince(DateTimeOffset date)
        {
            throw new NotImplementedException();
        }

        public Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }
    }
}
