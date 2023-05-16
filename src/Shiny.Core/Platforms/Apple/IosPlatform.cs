using System;
using System.Reactive.Linq;
using System.IO;
using System.Linq;
using Foundation;

namespace Shiny;


public class IosPlatform : IPlatform
{
    public IosPlatform()
    {
        this.AppData = ToDirectory(NSSearchPathDirectory.LibraryDirectory);
        this.Public = ToDirectory(NSSearchPathDirectory.DocumentDirectory);
        this.Cache = ToDirectory(NSSearchPathDirectory.CachesDirectory);
    }

    static DirectoryInfo ToDirectory(NSSearchPathDirectory dir) => new DirectoryInfo(NSSearchPath.GetDirectories(dir, NSSearchPathDomain.User).First());
    public DirectoryInfo AppData { get; }
    public DirectoryInfo Cache { get; }
    public DirectoryInfo Public { get; }
    public string AppIdentifier => NSBundle.MainBundle.BundleIdentifier;

    //public string AppVersion => NSBundle.MainBundle.InfoDictionary["CFBundleVersion"].ToString();
    //public string AppBuild => NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();

    //public string MachineName { get; } = "";
    //public string OperatingSystem => NSProcessInfo.ProcessInfo.OperatingSystemName;
    //public string OperatingSystemVersion => NSProcessInfo.ProcessInfo.OperatingSystemVersionString;
    //public string Manufacturer { get; } = "Apple";
    //public string Model { get; } = "";


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
