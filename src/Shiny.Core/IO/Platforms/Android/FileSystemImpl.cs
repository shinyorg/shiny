using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;


namespace Shiny.IO
{
    public class FileSystemImpl : AbstractFileSystemImpl
    {
        public FileSystemImpl(AndroidContext context)
        {
            this.AppData = new DirectoryInfo(context.AppContext.FilesDir.AbsolutePath);
            this.Cache = new DirectoryInfo(context.AppContext.CacheDir.AbsolutePath);
            var publicDir = context.AppContext.GetExternalFilesDir(null);
            if (publicDir != null)
                this.Public = new DirectoryInfo(publicDir.AbsolutePath);
        }


        public override IObservable<FileSystemEvent> Watch(string path, string filter = "*.*") => Observable.Create<FileSystemEvent>(ob =>
        {
            var obs = new ShinyFileObserver(path, ob.OnNext);
            return Disposable.Empty;
        });

        //public string ToFileUri(string path) => "file:/" + path;
    }
}