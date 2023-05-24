using System;
using System.IO;

using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;

namespace Shiny;


public class WindowsPlatform : IPlatform
{
    public WindowsPlatform()
    {
        var path = ApplicationData.Current.LocalFolder.Path;
        this.AppData = new DirectoryInfo(path);
        this.Cache = new DirectoryInfo(Path.Combine(path, "Cache"));
        this.Public = new DirectoryInfo(Path.Combine(path, "Public"));
    }


    public DirectoryInfo AppData { get; }

    public DirectoryInfo Cache { get; }

    public DirectoryInfo Public { get; }

    public void InvokeOnMainThread(Action action)
    {
        var dispatcher = CoreApplication.MainView.CoreWindow?.Dispatcher;

        if (dispatcher == null)
            throw new NullReferenceException("Main thread missing");

        if (dispatcher.HasThreadAccess)
            action();
        else
            dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
    }
}
