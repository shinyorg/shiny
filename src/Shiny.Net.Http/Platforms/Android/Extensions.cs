using System;
using Android.Content;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    static class Extensions
    {
        static Native downloadManager;
        public static Native GetManager(this AndroidContext context)
            => context.AppContext.GetManager();


        public static Native GetManager(this Context context)
        {
            if (downloadManager == null || downloadManager.Handle == IntPtr.Zero)
                downloadManager = (Native)context.GetSystemService(Context.DownloadService);

            return downloadManager;
        }
    }
}
