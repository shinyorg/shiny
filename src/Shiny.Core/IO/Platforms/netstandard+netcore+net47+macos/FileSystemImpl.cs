using System;
using System.IO;
using Shiny.IO;


namespace Shiny
{
    public class FileSystemImpl : AbstractFileSystemImpl
    {
        public FileSystemImpl()
        {
            this.AppData = this.Cache = this.Public = new DirectoryInfo(".");
        }
    }
}