using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Photos;


namespace Shiny.MediaSync.Infrastructure
{
    public class MediaGalleryScannerImpl : IMediaGalleryScanner
    {
        public Task<IEnumerable<MediaAsset>> Query(MediaTypes mediaTypes, DateTimeOffset date)
        {
            var tcs = new TaskCompletionSource<IEnumerable<MediaAsset>>();
            Task.Run(() =>
            { 
                var fetchOptions = new PHFetchOptions
                {
                    Predicate = NSPredicate.FromFormat(
                        $"mediaType == {(int)PHAssetMediaType.Image} || mediaType == {(int)PHAssetMediaType.Video}"
                    ),
                    SortDescriptors = new [] { 
                        new NSSortDescriptor("creationDate", false) 
                    }
                };
                var results = PHAsset
                    .FetchAssets(fetchOptions)
                    .OfType<PHAsset>();

                var assets = new List<MediaAsset>(results.Count());
                foreach (var result in results)
                {
                    //result.MediaType
                    //result.Hidden
                    //result.Duration
                    //result.LocalIdentifier
                    //result.Location;
                    //result.ModificationDate
                    //result.PixelHeight;
                    //result.PixelWidth;
                    //result.CreationDate

                    var resources = PHAssetResource.GetAssetResources(result);
                    foreach (var resource in resources)
                    {
                        //assets.Add(new MediaAsset(
                            
                        //));
                        //resource.OriginalFilename
                    }
                }
                tcs.SetResult(assets);
            });
            return tcs.Task;
        }


        public async Task<AccessState> RequestAccess()
        {
            var status = PHPhotoLibrary.AuthorizationStatus;
            if (status == PHAuthorizationStatus.NotDetermined)
                status = await PHPhotoLibrary.RequestAuthorizationAsync();

            switch (status)
            {
                case PHAuthorizationStatus.Authorized:
                    return AccessState.Available;

                case PHAuthorizationStatus.Denied:
                    return AccessState.Denied;

                case PHAuthorizationStatus.Restricted:
                    return AccessState.Restricted;

                default:
                    throw new ArgumentException("Invalid status - " + status);
            }
        }
    }
}