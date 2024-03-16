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

    //macCatalyst 13.0 = macOS 10.15 (Catalina)
    //macCatalyst 13.4 = macOS 10.15.4
    //macCatalyst 14.0 = macOS 11.0 (Big Sur)
    //macCatalyst 14.7 = macOS 11.6
    //macCatalyst 15.0 = macOS 12.0 (Monterey)
    //macCatalyst 15.3 = macOS 12.2 and 12.2.1
    //macCatalyst 15.4 = macOS 12.3
    //macCatalyst 15.5 = macOS 12.4
    //macCatalyst 15.6 = macOS 12.5
    public static bool IsAppleVersionAtleast(int osMajor, int osMinor = 0)
        => OperatingSystem.IsIOSVersionAtLeast(osMajor, osMinor) || OperatingSystem.IsMacCatalystVersionAtLeast(osMajor, osMinor);

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
