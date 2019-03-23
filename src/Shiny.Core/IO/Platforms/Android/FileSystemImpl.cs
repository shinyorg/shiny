using System;
using System.IO;


namespace Shiny.IO
{
    public class FileSystemImpl : IFileSystem
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


        public DirectoryInfo AppData { get; set; }
        public DirectoryInfo Cache { get; set; }
        public DirectoryInfo Public { get; set; }

        public string ToFileUri(string path) => "file:/" + path;
    }
}