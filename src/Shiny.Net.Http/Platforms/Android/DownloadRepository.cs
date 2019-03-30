using System;
using Android.App;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http.Platforms.Android
{
    public static class DownloadRepository
    {

        //static Native downloadManager;
        //public static Native GetManager()
        //{
        //    if (this.downloadManager == null || this.downloadManager.Handle == IntPtr.Zero)
        //        this.downloadManager = (Native)this.context.AppContext.GetSystemService(Context.DownloadService);

        //    return this.downloadManager;
        //}


        public static void GetAll()
        {
            //using (var cursor = this.GetManager().InvokeQuery(query))
            //{
            //    while (cursor.MoveToNext())
            //    {

            //    }
            //}
        }

        public static void Get(long id)
        {
            var query = new Native.Query();
            query.SetFilterById(id);
            //(DownloadStatus)cursor.GetInt(cursor.GetColumnIndex(Native.ColumnStatus))

        }
    }
}
