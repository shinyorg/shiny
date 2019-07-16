using System;
using System.IO;
using System.Linq;
using Foundation;


namespace Shiny.IO
{
    public class FileSystemImpl : AbstractFileSystemImpl
    {
        public FileSystemImpl()
        {
            this.AppData = ToDirectory(NSSearchPathDirectory.LibraryDirectory);
            this.Public = ToDirectory(NSSearchPathDirectory.DocumentDirectory);
            this.Cache = ToDirectory(NSSearchPathDirectory.CachesDirectory);
        }


        static DirectoryInfo ToDirectory(NSSearchPathDirectory dir)
            => new DirectoryInfo(NSSearchPath.GetDirectories(dir, NSSearchPathDomain.User).First());
    }
}