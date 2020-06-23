using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AVFoundation;
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
                var filter = "creationDate >= %@ && " + GetFilter(mediaTypes);

                var fetchOptions = new PHFetchOptions
                {
                    IncludeHiddenAssets = false,
                    IncludeAllBurstAssets = false,
                    Predicate = NSPredicate.FromFormat(filter, (NSDate)date.LocalDateTime),
                    SortDescriptors = new [] { 
                        new NSSortDescriptor("creationDate", false) 
                    }
                };
                
                var fetchAssets = PHAsset
                    .FetchAssets(fetchOptions)
                    .OfType<PHAsset>()
                    .ToArray();

                var imageCache = new PHCachingImageManager();
                imageCache.StartCaching(
                    fetchAssets,
                    PHImageManager.MaximumSize,
                    PHImageContentMode.Default,
                    new PHImageRequestOptions
                    {
                        NetworkAccessAllowed = false
                    }
                );

                //result.MediaType
                //result.Hidden
                //result.Duration
                //result.LocalIdentifier
                //result.Location;
                //result.ModificationDate
                //result.PixelHeight;
                //result.PixelWidth;
                //result.CreationDate
                var assets = new List<MediaAsset>(fetchAssets.Count());
                foreach (var asset in fetchAssets)
                {
                    switch (asset.MediaType)
                    {
                        case PHAssetMediaType.Image:
                            asset.RequestContentEditingInput(
                                new PHContentEditingInputRequestOptions
                                {
                                    NetworkAccessAllowed = false
                                },
                                (input, info) =>
                                {
                                    assets.Add(new MediaAsset(
                                        asset.LocalIdentifier,
                                        input.FullSizeImageUrl.ToString(),
                                        MediaTypes.Image 
                                    ));
                                }
                            );
                            break;

                        case PHAssetMediaType.Audio:
                        case PHAssetMediaType.Video:
                            imageCache.RequestAvAsset(
                                asset,
                                new PHVideoRequestOptions
                                {
                                    NetworkAccessAllowed = false,
                                    DeliveryMode = PHVideoRequestOptionsDeliveryMode.HighQualityFormat
                                },
                                (ass, mix, dict) =>
                                {
                                    if (ass is AVUrlAsset url)
                                    {
                                        var path = url.Url.ToString().Replace("file://", String.Empty);
                                        assets.Add(new MediaAsset(
                                            asset.LocalIdentifier,
                                            path,
                                            asset.MediaType == PHAssetMediaType.Audio
                                                ? MediaTypes.Audio
                                                : MediaTypes.Video
                                        ));
                                    }
                                }
                            );
                            break;
                    }
                }
                imageCache.StopCaching();
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


        static string GetFilter(MediaTypes mediaTypes)
        {
            var filters = new List<string>(3);
            if (mediaTypes.HasFlag(MediaTypes.Image))
                filters.Add("mediaType == " + (int)PHAssetMediaType.Image);
            
            if (mediaTypes.HasFlag(MediaTypes.Audio))
                filters.Add("mediaType == " + (int)PHAssetMediaType.Audio);
            
            if (mediaTypes.HasFlag(MediaTypes.Video))
                filters.Add("mediaType == " + (int)PHAssetMediaType.Video);

            return String.Join(" || ", filters.ToArray());
        }


        static MediaTypes ToMediaType(PHAssetMediaType type) => type switch
        {
            PHAssetMediaType.Audio => MediaTypes.Audio,
            PHAssetMediaType.Video => MediaTypes.Video,
            PHAssetMediaType.Image => MediaTypes.Image
        };
    }
}