using System;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    static class Extensions
    {
        public static Android.Net.Uri ToNativeUri(this FileInfo file)
        {
            var native = new Java.IO.File(file.FullName);
            return Android.Net.Uri.FromFile(native);
        }


        public static Native.Query ToNative(this QueryFilter filter)
        {
            var query = new Native.Query();
            if (filter != null)
            {
                if (filter.Ids?.Any() ?? false)
                {
                    var ids = filter.Ids.Select(long.Parse).ToArray();
                    query.SetFilterById(ids);
                }
                var flags = DownloadStatus.Pending | DownloadStatus.Running;
                //if (!filter.IncludeInProgress)
                //    flags ~= DownloadStatus.Running;

                //if (!filter.IncludePending)
                //    flags ~= DownloadStatus.Pending;

                query.SetFilterByStatus(flags);
            }
            return query;
        }


        static Native downloadManager;
        public static Native GetManager(this IAndroidContext context)
        {
            if (downloadManager == null || downloadManager.Handle == IntPtr.Zero)
                downloadManager = (Native)context.AppContext.GetSystemService(Context.DownloadService);

            return downloadManager;
        }
    }
}
