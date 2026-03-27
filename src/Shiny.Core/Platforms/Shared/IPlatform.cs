using System;
using System.IO;

namespace Shiny;


/// <summary>
/// Provides platform-specific operations and directory paths
/// </summary>
public interface IPlatform
{
    //string AppIdentifier { get; }

    /// <summary>
    /// Invokes an action on the platform main/UI thread
    /// </summary>
    /// <param name="action">The action to invoke</param>
    void InvokeOnMainThread(Action action);

    /// <summary>
    /// Gets the application data directory
    /// </summary>
    DirectoryInfo AppData { get; }

    /// <summary>
    /// Gets the cache directory
    /// </summary>
    DirectoryInfo Cache { get; }

    /// <summary>
    /// Gets the public/shared directory
    /// </summary>
    DirectoryInfo Public { get; }
}
