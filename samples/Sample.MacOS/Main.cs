using AppKit;
using Microsoft.Maui.Platform.MacOS.Hosting;

namespace Sample.MacOS;

public class Program
{
    static void Main(string[] args)
    {
        NSApplication.Init();
        NSApplication.SharedApplication.Delegate = new MacOSMauiApplicationDelegate();
        NSApplication.Main(args);
    }
}

public class MacOSMauiApplicationDelegate : MacOSMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
