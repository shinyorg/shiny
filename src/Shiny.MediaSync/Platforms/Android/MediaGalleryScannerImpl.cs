using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.App;
using Android.Database;
using Android.Provider;
using static Android.Manifest;

[assembly: UsesPermission("android.permission.ACCESS_MEDIA_LOCATION")]
[assembly: UsesPermission(Permission.ReadExternalStorage)]


namespace Shiny.MediaSync.Infrastructure
{
    public class MediaGalleryScannerImpl : IMediaGalleryScanner
    {
        readonly AndroidContext context;
        public MediaGalleryScannerImpl(AndroidContext context)
            => this.context = context;


        public Task<IEnumerable<MediaAsset>> Query(MediaTypes mediaTypes, DateTimeOffset date)
        {
            var tcs = new TaskCompletionSource<IEnumerable<MediaAsset>>();
            Task.Run(() =>
            {
                var uri = MediaStore.Files.GetContentUri("external");
                var cursor = this.context.AppContext.ContentResolver.Query(
                    uri,
                    new [] 
                    {
                        MediaStore.Files.FileColumns.Id,
                        MediaStore.Files.FileColumns.Data,
                        MediaStore.Files.FileColumns.DateAdded,
                        MediaStore.Files.FileColumns.MediaType,
                        MediaStore.Files.FileColumns.MimeType,
                        MediaStore.Files.FileColumns.Title,
                        MediaStore.Files.FileColumns.Parent,
                        MediaStore.Files.FileColumns.DisplayName,
                        MediaStore.Files.FileColumns.Size
                    }, 
                    ToWhereClause(mediaTypes, date),
                    null, 
                    $"{MediaStore.Files.FileColumns.DateAdded} DESC"
                );

                var list = new List<MediaAsset>();
                while (cursor.MoveToNext())
                    list.Add(ToAsset(cursor));

                tcs.SetResult(list);
            });
            return tcs.Task;
        }


        public Task<AccessState> RequestAccess() => this.context
            .RequestAccess(
                Permission.ReadExternalStorage, 
                "android.permission.ACCESS_MEDIA_LOCATION"
            )
            .ToTask();


        static string ToWhereClause(MediaTypes mediaTypes, DateTimeOffset date)
        {
            var filters = new List<string>(4);

            if (mediaTypes.HasFlag(MediaTypes.Image))
                filters.Add($"{MediaStore.Files.FileColumns.MediaType} = {(int)MediaType.Image}");

            if (mediaTypes.HasFlag(MediaTypes.Video))
                filters.Add($"{MediaStore.Files.FileColumns.MediaType} = {(int)MediaType.Video}");

            if (mediaTypes.HasFlag(MediaTypes.Audio))
                filters.Add($"{MediaStore.Files.FileColumns.MediaType} = {(int)MediaType.Audio}");

            var filter = String.Join(" OR ", filters.ToArray());

            return filter;
        }


        static MediaAsset ToAsset(ICursor cursor)
        {
            var id = cursor.GetInt(cursor.GetColumnIndex(MediaStore.Files.FileColumns.Id));
            var type = cursor.GetInt(cursor.GetColumnIndex(MediaStore.Files.FileColumns.MediaType));
            var path = cursor.GetString(cursor.GetColumnIndex(MediaStore.Files.FileColumns.Data));

            var mediaType = MediaTypes.None;
            switch (type)
            {
                case (int)MediaType.Audio:
                    mediaType = MediaTypes.Audio;
                    break;

                case (int)MediaType.Image:
                    mediaType = MediaTypes.Image;
                    break;

                case (int)MediaType.Video:
                    mediaType = MediaTypes.Video;
                    break;
            }

            return new MediaAsset(
                id.ToString(),
                path,
                mediaType
            );
        }
    }
}
/*
var id = cursor.GetInt(cursor.GetColumnIndex(MediaStore.Files.FileColumns.Id));
                                var mediaType = cursor.GetInt(cursor.GetColumnIndex(MediaStore.Files.FileColumns.MediaType));
                                Bitmap bitmap = null;
                                switch (mediaType)
                                {
                                    case (int)MediaType.Image:
                                        bitmap = MediaStore.Images.Thumbnails.GetThumbnail(CrossCurrentActivity.Current.Activity.ContentResolver, id, ThumbnailKind.MiniKind, new BitmapFactory.Options()
                                        {
                                            InSampleSize = 4,
                                            InPurgeable = true
                                        });
                                        break;
                                    case (int)MediaType.Video:
                                        bitmap = MediaStore.Video.Thumbnails.GetThumbnail(CrossCurrentActivity.Current.Activity.ContentResolver, id, VideoThumbnailKind.MiniKind, new BitmapFactory.Options()
                                        {
                                            InSampleSize = 4,
                                            InPurgeable = true
                                        });
                                        break;
                                }
                                var tmpPath = System.IO.Path.GetTempPath();
                                var name = GetString(cursor, MediaStore.Files.FileColumns.DisplayName);
                                var filePath = System.IO.Path.Combine(tmpPath, $"tmp_{name}");

                                var path = GetString(cursor, MediaStore.Files.FileColumns.Data);

                                //filePath = path;
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    bitmap?.Compress(Bitmap.CompressFormat.Png, 100, stream);
                                    stream.Close();
                                }


                                if (!string.IsNullOrWhiteSpace(filePath))
                                {
                                    var asset = new MediaAsset()
                                    {
                                        Id = $"{id}",
                                        Type = mediaType == (int)MediaType.Video ? MediaAssetType.Video : MediaAssetType.Image,
                                        Name = name,
                                        PreviewPath = filePath,
                                        Path = path
                                    };
                                  
                                    using (var h = new Handler(Looper.MainLooper))
                                            h.Post(async () => {  OnMediaAssetLoaded?.Invoke(this, new MediaEventArgs(asset)); });

                                    assets.Add(asset);
                                }
 */
