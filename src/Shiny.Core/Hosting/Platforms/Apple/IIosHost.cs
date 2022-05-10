using System;
using System.IO;

namespace Shiny.Hosting;

public interface IIosHost : IHost
{
    //string AppIdentifier { get; }
    void InvokeOnMainThread(Action action);

    DirectoryInfo AppData { get; }
    DirectoryInfo Cache { get; }
    DirectoryInfo Public { get; }
}