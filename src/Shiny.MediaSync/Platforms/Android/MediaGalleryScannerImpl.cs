using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Database;
using Android.Provider;
using static Android.Manifest;


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


        public Task<AccessState> RequestAccess()
        {
            //this.context.RequestAccess(Permission.WriteExternalStorage, 1354);
            throw new NotImplementedException();
        }


        static string ToWhereClause(MediaTypes mediaTypes, DateTimeOffset date)
        {
            return $"{MediaStore.Files.FileColumns.MediaType} = {(int)MediaType.Image} OR {MediaStore.Files.FileColumns.MediaType} = {(int)MediaType.Video}";
        }


        static MediaAsset ToAsset(ICursor cursor)
        {
            var id = cursor.GetInt(cursor.GetColumnIndex(MediaStore.Files.FileColumns.Id));
            var type = cursor.GetInt(cursor.GetColumnIndex(MediaStore.Files.FileColumns.MediaType));
            return null;
        }
    }
}