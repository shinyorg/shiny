using System;
using System.IO;

namespace Shiny;


/// <summary>
/// The <see cref="IPlatform"/> for hosts that are just .NET - a GTK desktop app on Linux, a console
/// app, a service - where there is no platform SDK to ask for the directories.
/// </summary>
/// <remarks>
/// <para>
/// It exists because the base target framework had no <see cref="IPlatform"/> at all, which meant
/// anything built on Shiny.Core could not be compiled for it: a Linux desktop head cannot ask for a
/// net10.0-linux build, because there is no such target framework, so whatever it gets has to come
/// from the plain net10.0 one.
/// </para>
/// <para>
/// Modelled on <c>WindowsPlatform</c>'s unpackaged branch rather than on anything new, and that is
/// deliberate - the question "where does an app with no container put its files" has the same
/// answer on both, and two answers to it would be two layouts to support.
/// </para>
/// </remarks>
public class NetPlatform : IPlatform
{
    public NetPlatform()
    {
        // LocalApplicationData is already the XDG answer on Linux: .NET resolves it to
        // $XDG_DATA_HOME, falling back to ~/.local/share. So an app folder underneath it is
        // XDG-correct without this class having to know what XDG is.
        //
        // Named after the entry assembly, because everything sharing this root would otherwise
        // write straight into a directory the whole desktop shares.
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDomain.CurrentDomain.FriendlyName
        );
        Directory.CreateDirectory(path);

        this.AppData = new DirectoryInfo(path);
        this.Cache = new DirectoryInfo(Path.Combine(path, "Cache"));
        this.Public = new DirectoryInfo(Path.Combine(path, "Public"));
    }


    public DirectoryInfo AppData { get; }

    public DirectoryInfo Cache { get; }

    public DirectoryInfo Public { get; }


    /// <summary>
    /// Set by the hosting layer at app launch so that <see cref="InvokeOnMainThread"/> can marshal
    /// onto whatever that host calls its main thread - the GTK main loop, for instance - without
    /// Shiny.Core taking a dependency on a UI toolkit it otherwise knows nothing about.
    /// </summary>
    /// <remarks>
    /// The same hook, spelled the same way, as <c>WindowsPlatform.MainThreadHandler</c>. A host that
    /// does not set it is not broken: plenty of things built on this have no main thread to marshal
    /// to, which is why the fallback below runs the action rather than throwing.
    /// </remarks>
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
