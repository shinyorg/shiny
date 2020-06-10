using System;
using System.Threading.Tasks;

namespace Shiny.PhotoSync
{
    public class PhotoGalleryScannerImpl : IPhotoGalleryScanner
    {
        public Task<Photo> GetPhotosSince(DateTime? date)
        {
            throw new NotImplementedException();
        }
    }
}
