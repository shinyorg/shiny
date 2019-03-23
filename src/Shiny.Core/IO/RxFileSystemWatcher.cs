using System;
using System.IO;
using System.Reactive.Linq;

namespace Shiny.IO
{
    public class RxFileSystemWatcher : IDisposable
    {
        readonly FileSystemWatcher watcher;
        readonly IObservable<FileSystemEvent> changed;
        readonly IObservable<Exception> error;


        public RxFileSystemWatcher(string path, string filter = null)
        {
            this.watcher = new FileSystemWatcher(path, filter);
            this.changed = Observable.Create<FileSystemEvent>(ob =>
            {
                this.watcher.EnableRaisingEvents = true;
                var handler = new FileSystemEventHandler((sender, args) =>
                    ob.OnNext(new FileSystemEvent(args.ChangeType, args.Name, args.FullPath))
                );
                var renameHandler = new RenamedEventHandler((sender, args) =>
                    ob.OnNext(new FileSystemEvent(args.ChangeType, args.Name, args.FullPath, args.OldName, args.OldFullPath))
                );
                this.watcher.Created += handler;
                this.watcher.Changed += handler;
                this.watcher.Deleted += handler;
                this.watcher.Renamed += renameHandler;

                return () =>
                {
                    this.watcher.EnableRaisingEvents = false;
                    this.watcher.Created -= handler;
                    this.watcher.Changed -= handler;
                    this.watcher.Deleted -= handler;
                    this.watcher.Renamed -= renameHandler;
                };
            })
            .Publish()
            .RefCount();

            this.error = Observable.Create<Exception>(ob =>
            {
                var handler = new ErrorEventHandler((sender, args) =>
                    ob.OnNext(args.GetException())
                );
                this.watcher.Error += handler;
                return () => this.watcher.Error -= handler;
            });
        }


        public IObservable<FileSystemEvent> WhenChanged() => this.changed;
        public IObservable<Exception> WhenErrorOccurred() => this.error;
        public void Dispose() => this.watcher?.Dispose();
    }
}
