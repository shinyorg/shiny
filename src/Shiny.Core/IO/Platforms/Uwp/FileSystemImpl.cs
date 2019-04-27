using System;
using System.IO;


namespace Shiny.IO
{
    public class FileSystemImpl : AbstractFileSystemImpl
    {
        public FileSystemImpl()
        {
            var path = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            this.AppData = new DirectoryInfo(path);
            this.Cache = new DirectoryInfo(Path.Combine(path, "Cache"));
            this.Public = new DirectoryInfo(Path.Combine(path, "Public"));
        }
    }
}
