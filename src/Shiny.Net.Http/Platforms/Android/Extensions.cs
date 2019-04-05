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
                if (filter.States?.Any() ?? false)
                {
                    var stateFlags = 0;
                    foreach (var state in filter.States)
                        stateFlags += (int)state.ToNative();

                    var states = (DownloadStatus)stateFlags;
                    query.SetFilterByStatus(states);
                }
            }
            return query;
        }


        static DownloadStatus ToNative(this HttpTransferState state)
        {
            switch (state)
            {
                case HttpTransferState.Completed:
                    return DownloadStatus.Successful;

                default:
                    return DownloadStatus.Failed;
            }
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
