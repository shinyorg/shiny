using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;


namespace Shiny.MediaSync.Infrastructure
{
    public class MediaGalleryScannerImpl : IMediaGalleryScanner
    {
        //https://docs.microsoft.com/en-us/windows/uwp/files/quickstart-managing-folders-in-the-music-pictures-and-videos-libraries
        public async Task<IEnumerable<MediaAsset>> Query(MediaTypes mediaTypes, DateTimeOffset date)
        {
            var camera = await KnownFolders.CameraRoll.GetFilesAsync();
            var photos = await KnownFolders.PicturesLibrary.GetFilesAsync();
            var videos = await KnownFolders.VideosLibrary.GetFilesAsync();

            return Enumerable.Empty<MediaAsset>();
        }


        public async Task<AccessState> RequestAccess()
        {
            var photos = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            var videos = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);

            return AccessState.Available;
        }
    }
}