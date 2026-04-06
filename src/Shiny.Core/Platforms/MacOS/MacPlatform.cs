using System;
using System.IO;
using System.Linq;
using Foundation;

namespace Shiny;


public class MacPlatform : IPlatform
{
    public MacPlatform()
    {
        this.AppData = ToDirectory(NSSearchPathDirectory.LibraryDirectory);
        this.Public = ToDirectory(NSSearchPathDirectory.DocumentDirectory);
        this.Cache = ToDirectory(NSSearchPathDirectory.CachesDirectory);
    }


    static DirectoryInfo ToDirectory(NSSearchPathDirectory dir)
        => new DirectoryInfo(NSSearchPath.GetDirectories(dir, NSSearchPathDomain.User).First());


    public DirectoryInfo AppData { get; }
    public DirectoryInfo Cache { get; }
    public DirectoryInfo Public { get; }
    public string AppIdentifier => NSBundle.MainBundle.BundleIdentifier;


    public void InvokeOnMainThread(Action action)
    {
        if (NSThread.Current.IsMainThread)
        {
            action();
        }
        else
        {
            NSRunLoop.Main.BeginInvokeOnMainThread(action);
        }
    }
}
