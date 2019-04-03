using System;
using System.IO;
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


        static Native downloadManager;
        public static Native GetManager(this IAndroidContext context)
        {
            if (downloadManager == null || downloadManager.Handle == IntPtr.Zero)
                downloadManager = (Native)context.AppContext.GetSystemService(Context.DownloadService);

            return downloadManager;
        }
    }
}
