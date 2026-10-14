using System.Diagnostics;

namespace Shiny.DeviceTests.Runner;


public record ShellResult(int ExitCode, string StdOut, string StdErr)
{
    public bool Success => this.ExitCode == 0;
    public override string ToString() => $"exit {this.ExitCode}: {this.StdOut}{this.StdErr}".Trim();
}


public static class Shell
{
    public static async Task<ShellResult> RunAsync(string fileName, IEnumerable<string> args, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start {fileName}");
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return new ShellResult(process.ExitCode, await stdout, await stderr);
    }


    public static async Task<string> RunCheckedAsync(string fileName, IEnumerable<string> args, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(fileName, args, cancellationToken);
        if (!result.Success)
            throw new InvalidOperationException($"{fileName} {String.Join(' ', args)} failed - {result}");

        return result.StdOut;
    }


    public static string Adb
    {
        get
        {
            var sdk = Environment.GetEnvironmentVariable("ANDROID_HOME")
                ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Android", "sdk");

            var path = Path.Combine(sdk, "platform-tools", "adb");
            return File.Exists(path) ? path : "adb";
        }
    }
}
