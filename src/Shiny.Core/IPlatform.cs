using System;
using System.IO;


namespace Shiny
{
    public enum PlatformState
    {
        Foreground,
        Background
    }


    public interface IPlatform
    {
        string Name { get; }

        PlatformState Status { get; }
        DirectoryInfo AppData { get; }
        DirectoryInfo Cache { get; }
        DirectoryInfo Public { get; }

        string AppIdentifier { get; }
        string AppVersion { get; }
        string AppBuild { get; }

        string MachineName { get; }
        string OperatingSystem { get; }
        string OperatingSystemVersion { get; }
        string Manufacturer { get; }
        string Model { get; }

        void InvokeOnMainThread(Action action);
        IObservable<PlatformState> WhenStateChanged();
    }
}
