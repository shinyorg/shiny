using System.Diagnostics;

namespace Shiny.Printing;


/// <summary>Default <see cref="ICupsProcessRunner"/> that launches the tool via <see cref="Process"/>.</summary>
public sealed class CupsProcessRunner : ICupsProcessRunner
{
    public async Task<ProcessResult> Run(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        // lpstat translates its output ("printer X is idle", "system default destination:") and the
        // parser matches the English text, so pin the C locale
        psi.Environment["LC_ALL"] = "C";

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");

        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // don't leave lp / lpstat running behind a cancelled call
            try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
            throw;
        }

        return new ProcessResult(
            process.ExitCode,
            await stdout.ConfigureAwait(false),
            await stderr.ConfigureAwait(false)
        );
    }
}
