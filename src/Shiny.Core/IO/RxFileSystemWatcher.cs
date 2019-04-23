using System;
using System.IO;
using System.Reactive.Linq;

namespace Shiny.IO
{
    /// <summary>
    /// An RX version of the .NET FileSystemWatcher
    /// </summary>
    public class RxFileSystemWatcher : IDisposable
    {
        readonly FileSystemWatcher watcher;
        readonly IObservable<FileSystemEvent> changed;


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
                var errorHandler = new ErrorEventHandler((sender, args) =>
                    ob.OnError(args.GetException())
                );
                this.watcher.Created += handler;
                this.watcher.Changed += handler;
                this.watcher.Deleted += handler;
                this.watcher.Error += errorHandler;
                this.watcher.Renamed += renameHandler;

                return () =>
                {
                    this.watcher.EnableRaisingEvents = false;
                    this.watcher.Created -= handler;
                    this.watcher.Changed -= handler;
                    this.watcher.Deleted -= handler;
                    this.watcher.Renamed -= renameHandler;
                    this.watcher.Error -= errorHandler;
                };
            })
            .Publish()
            .RefCount();
        }


        /// <summary>
        /// Returns a FileSystemEvent for the provided path and filter
        /// </summary>
        /// <param name="path">The file system path to watch</param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IObservable<FileSystemEvent> Create(string path, string filter = null) => Observable.Create<FileSystemEvent>(ob =>
        {
            var watcher = new RxFileSystemWatcher(path, filter);
            var sub = watcher
                .WhenChanged()
                .Subscribe(
                    ob.OnNext,
                    ob.OnError,
                    ob.OnCompleted
                );

            return () =>
            {
                sub.Dispose();
                watcher.Dispose();
            };
        });


        /// <summary>
        /// The main method to subscribe to
        /// </summary>
        /// <returns></returns>
        public IObservable<FileSystemEvent> WhenChanged() => this.changed;
        public void Dispose() => this.watcher?.Dispose();
    }
}
