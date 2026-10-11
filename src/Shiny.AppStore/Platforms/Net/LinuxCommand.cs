using System.Diagnostics;

namespace Shiny.AppStore;

/// <summary>
/// Runs the freedesktop command line tools the Linux app-store integration depends on
/// (<c>flatpak</c>, <c>snap</c>, <c>xdg-open</c>) without ever throwing at the caller.
/// </summary>
static class LinuxCommand
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// True when the process is running inside a Flatpak sandbox. The <c>flatpak</c> CLI itself lives on
    /// the host and isn't visible in there, so commands have to be forwarded through <c>flatpak-spawn</c>.
    /// </summary>
    public static bool IsFlatpakSandbox { get; } = File.Exists("/.flatpak-info");


    /// <summary>
    /// Runs a command and returns its standard output, or null when the tool is missing, exits non-zero,
    /// or doesn't finish in time. Anything that can go wrong here is "we can't tell", not an error worth
    /// throwing at an app that just wanted to know whether an update exists.
    /// </summary>
    public static async Task<string?> Read(
        string fileName,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken = default
    )
    {
        var psi = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
                return null;

            // The timeout is the reason the output is read before the wait - a tool that fills the pipe
            // buffer and then blocks on the write would otherwise never reach exit.
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(DefaultTimeout);

            try
            {
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Kill(process);
                return null;
            }

            return process.ExitCode == 0 ? await stdout.ConfigureAwait(false) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }


    /// <summary>
    /// Runs a Flatpak CLI command, hopping out of the sandbox first when the app is itself a Flatpak.
    /// </summary>
    public static Task<string?> ReadFlatpak(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
    {
        if (!IsFlatpakSandbox)
            return Read("flatpak", arguments, cancellationToken);

        return Read("flatpak-spawn", new[] { "--host", "flatpak" }.Concat(arguments), cancellationToken);
    }


    /// <summary>
    /// Hands a URI to the desktop's default handler. Used rather than a generic URI launcher because
    /// <see cref="Uri"/> lower-cases the host, which would corrupt case-sensitive AppStream
    /// component IDs such as <c>appstream://org.gnome.Calculator</c>.
    /// </summary>
    public static Task<bool> Open(string uri)
    {
        var psi = new ProcessStartInfo("xdg-open")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (IsFlatpakSandbox)
        {
            // xdg-open inside a sandbox routes through the portal, which can't open appstream: URIs.
            psi.FileName = "flatpak-spawn";
            psi.ArgumentList.Add("--host");
            psi.ArgumentList.Add("xdg-open");
        }
        psi.ArgumentList.Add(uri);

        try
        {
            using var process = Process.Start(psi);
            return Task.FromResult(process != null);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }


    static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(true);
        }
        catch (Exception)
        {
            // the process exited between the check and the kill - nothing to clean up
        }
    }
}
