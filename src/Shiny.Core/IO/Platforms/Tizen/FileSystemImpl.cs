using System;
using Tizen.Applications;
using DirectoryInfo = System.IO.DirectoryInfo;


namespace Shiny.IO
{
    public class FileSystemImpl : IFileSystem
    {
        public FileSystemImpl()
        {
            this.AppData = new DirectoryInfo(Application.Current.DirectoryInfo.Data);
            this.Cache = new DirectoryInfo(Application.Current.DirectoryInfo.Cache);
            this.Public = new DirectoryInfo(Application.Current.DirectoryInfo.ExternalSharedData);
        }


        public DirectoryInfo AppData { get; set; }
        public DirectoryInfo Cache { get; set; }
        public DirectoryInfo Public { get; set; }
    }
}
