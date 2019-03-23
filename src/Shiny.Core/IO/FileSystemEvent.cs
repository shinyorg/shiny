using System;
using System.IO;


namespace Shiny.IO
{
    public class FileSystemEvent
    {
        public FileSystemEvent(WatcherChangeTypes change,
                              string name,
                              string fullPath,
                              string oldName = null,
                              string oldFullPath = null)
        {
            this.Event = change;
            this.Name = name;
            this.FullPath = fullPath;
            this.OldName = oldName;
            this.OldFullPath = oldFullPath;
        }


        public WatcherChangeTypes Event { get; }
        public string FullPath { get; }
        public string Name { get; }
        public string OldFullPath { get; }
        public string OldName { get; }
    }
}
