using System;
using System.IO;


namespace Shiny.IO
{
    public class FileSystemImpl : IFileSystem
    {
        public FileSystemImpl()
        {
            var path = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            this.AppData = new DirectoryInfo(path);
            this.Cache = new DirectoryInfo(Path.Combine(path, "Cache"));
            this.Public = new DirectoryInfo(Path.Combine(path, "Public"));
        }


        public DirectoryInfo AppData { get; set; }
        public DirectoryInfo Cache { get; set; }
        public DirectoryInfo Public { get; set; }
        public string ToFileUri(string path) => path; // TODO: need that ms-appx shit
    }
}
