using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.PhotoSync
{
    public class PhotoGalleryScannerImpl : IPhotoGalleryScanner
    {
        public Task<IEnumerable<Photo>> GetPhotosSince(DateTime? date)
        {
            throw new NotImplementedException();
        }
    }
}
