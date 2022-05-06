using System;
using System.IO;

namespace Shiny;


public interface IPlatform
{
    void InvokeOnMainThread(Action action);

    DirectoryInfo AppData { get; }
    DirectoryInfo Cache { get; }
    DirectoryInfo Public { get; }
}