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
                switch (filter.States)
                {
                    case HttpTransferStateFilter.Both:
                        query.SetFilterByStatus(DownloadStatus.Pending | DownloadStatus.Running);
                        break;

                    case HttpTransferStateFilter.Pending:
                        query.SetFilterByStatus(DownloadStatus.Pending);
                        break;

                    case HttpTransferStateFilter.InProgress:
                        query.SetFilterByStatus(DownloadStatus.Running);
                        break;
                }
            }
            return query;
        }


        static Native downloadManager;
        public static Native GetManager(this IAndroidContext context)
            => context.AppContext.GetManager();

        public static Native GetManager(this Context context)
        {
            if (downloadManager == null || downloadManager.Handle == IntPtr.Zero)
                downloadManager = (Native)context.GetSystemService(Context.DownloadService);

            return downloadManager;
        }
    }
}
