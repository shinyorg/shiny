using System;
using System.IO;
using System.Linq;
using CoreFoundation;
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


    // the main dispatch queue, the same as Shiny.Maui.Shell's macOS MainThread - MAUI Essentials'
    // MainThread has no AppKit implementation, so both marshal through GCD directly
    public void InvokeOnMainThread(Action action)
    {
        if (NSThread.IsMain)
            action();
        else
            DispatchQueue.MainQueue.DispatchAsync(action);
    }
}
