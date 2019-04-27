using System;
using System.IO;
using System.Reactive.Linq;

namespace Shiny.IO
{
    public class AbstractFileSystemImpl : IFileSystem
    {
        public DirectoryInfo AppData { get; set; }
        public DirectoryInfo Cache { get; set; }
        public DirectoryInfo Public { get; set; }


        public virtual IObservable<FileSystemEvent> Watch(string path, string filter = "*.*") => Observable.Create<FileSystemEvent>(ob =>
        {
            var watcher = new FileSystemWatcher(path, filter)
            {
                EnableRaisingEvents = true
            };
            var handler = new FileSystemEventHandler((sender, args) =>
                ob.OnNext(new FileSystemEvent(args.ChangeType, args.Name, args.FullPath))
            );
            var renameHandler = new RenamedEventHandler((sender, args) =>
                ob.OnNext(new FileSystemEvent(args.ChangeType, args.Name, args.FullPath, args.OldName, args.OldFullPath))
            );
            var errorHandler = new ErrorEventHandler((sender, args) =>
                ob.OnError(args.GetException())
            );
            watcher.Created += handler;
            watcher.Changed += handler;
            watcher.Deleted += handler;
            watcher.Error += errorHandler;
            watcher.Renamed += renameHandler;

            return () =>
            {
                watcher.EnableRaisingEvents = false;
                watcher.Created -= handler;
                watcher.Changed -= handler;
                watcher.Deleted -= handler;
                watcher.Renamed -= renameHandler;
                watcher.Error -= errorHandler;
                watcher.Dispose();
            };
        });
    }
}
