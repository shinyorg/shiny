using System;
using System.IO;
using Windows.Storage;

namespace Shiny;


public class WindowsPlatform : IPlatform
{
    public WindowsPlatform()
    {
        string path;
        try
        {
            path = ApplicationData.Current.LocalFolder.Path;
        }
        catch (InvalidOperationException)
        {
            // Unpackaged WinUI app - no package identity, so ApplicationData.Current is unavailable.
            var appName = AppDomain.CurrentDomain.FriendlyName;
            path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName
            );
            Directory.CreateDirectory(path);
        }
        this.AppData = new DirectoryInfo(path);
        this.Cache = new DirectoryInfo(Path.Combine(path, "Cache"));
        this.Public = new DirectoryInfo(Path.Combine(path, "Public"));
    }


    public DirectoryInfo AppData { get; }

    public DirectoryInfo Cache { get; }

    public DirectoryInfo Public { get; }

    /// <summary>
    /// Set by the hosting layer (e.g. Shiny.Hosting.Maui) at app launch so that
    /// <see cref="InvokeOnMainThread"/> can marshal to the WinUI 3 dispatcher without
    /// Shiny.Core taking a dependency on Microsoft.UI / WindowsAppSDK / MAUI.
    /// </summary>
    public static Action<Action>? MainThreadHandler { get; set; }

    public void InvokeOnMainThread(Action action)
    {
        var handler = MainThreadHandler;
        if (handler == null)
        {
            // No hosting layer wired this up - best-effort inline execution.
            action();
            return;
        }
        handler(action);
    }
}
