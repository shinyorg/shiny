using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.PhotoSync
{
    public interface IPhotoGalleryScanner
    {
        Task<IEnumerable<Photo>> GetPhotosSince(DateTime? date);
    }
}
