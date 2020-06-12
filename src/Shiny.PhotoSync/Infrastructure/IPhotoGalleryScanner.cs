using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.PhotoSync.Infrastructure
{
    public interface IPhotoGalleryScanner
    {
        Task<AccessState> RequestAccess();
        Task<IEnumerable<Photo>> GetPhotosSince(DateTime? date);
    }
}
