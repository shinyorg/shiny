using System;
using System.IO;

namespace Shiny;


public interface IPlatform
{
    //string AppIdentifier { get; }
    void InvokeOnMainThread(Action action);

    DirectoryInfo AppData { get; }
    DirectoryInfo Cache { get; }
    DirectoryInfo Public { get; }
}
