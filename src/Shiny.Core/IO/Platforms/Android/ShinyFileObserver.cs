using System;
using System.IO;
using Android.OS;
using Android.Runtime;


namespace Shiny.IO
{
    public class ShinyFileObserver : FileObserver
    {
        readonly Action<FileSystemEvent> action;
        public ShinyFileObserver(string path, Action<FileSystemEvent> action) : base(path)
            => this.action = action;


        public override void OnEvent([GeneratedEnum] FileObserverEvents e, string path)
        {
            WatcherChangeTypes? change = null;
            switch (e)
            {
                case FileObserverEvents.Access:
                    break;

                case FileObserverEvents.Attrib:
                    break;

                case FileObserverEvents.Create:
                    break;

                case FileObserverEvents.Delete:
                case FileObserverEvents.DeleteSelf:
                    break;

                case FileObserverEvents.Modify:
                    break;
            }
            //if (change != null)
                //ob.OnNext(new FileSystemEvent(WatcherChangeTypes.Created, path, null, null));
        }
    }
}
