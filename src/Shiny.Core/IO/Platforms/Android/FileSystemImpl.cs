using System;
using System.IO;
using System.Reactive.Linq;
using Android.OS;


namespace Shiny.IO
{
    public class FileSystemImpl : AbstractFileSystemImpl
    {
        public FileSystemImpl()
        {
            var ctx = Android.App.Application.Context;

            this.AppData = new DirectoryInfo(ctx.FilesDir.AbsolutePath);
            this.Cache = new DirectoryInfo(ctx.CacheDir.AbsolutePath);
            var publicDir = ctx.GetExternalFilesDir(null);
            if (publicDir != null)
                this.Public = new DirectoryInfo(publicDir.AbsolutePath);
        }


        public override IObservable<FileSystemEvent> Watch(string path, string filter = "*.*")
        {
            //var obs = new FileObserver("", FileObserverEvents.MovedFrom);

            return Observable.Empty<FileSystemEvent>();
        }

        //public string ToFileUri(string path) => "file:/" + path;
    }
}