using System.Diagnostics;

namespace Shiny.ScreenRecorder.Encoders;


/// <summary>
/// Finds out what this machine can actually encode with, and which audio device to record from.
/// </summary>
/// <remarks>
/// Everything here is probed rather than assumed. A Linux desktop may have GStreamer with no
/// x264 plugin, FFmpeg with no PulseAudio support, or PipeWire with no portal - and the difference
/// between them decides what <see cref="ScreenRecorderCapabilities"/> is allowed to claim.
/// </remarks>
internal static class EncoderProbe
{
    /// <summary>Whether a binary is on PATH and runs.</summary>
    public static bool Exists(string fileName)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(fileName)
            {
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            if (process == null)
                return false;

            // a version check that has not returned in two seconds is not a working binary
            if (!process.WaitForExit(2000))
            {
                try { process.Kill(true); } catch { /* already gone */ }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            // Win32Exception when the binary is not on PATH; anything else means it is unusable
            return false;
        }
    }


    /// <summary>Whether this is an X11 session, where the FFmpeg fallback can work.</summary>
    public static bool IsX11 =>
        !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) &&
        !String.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase);


    public static string Display => Environment.GetEnvironmentVariable("DISPLAY") ?? ":0.0";


    /// <summary>
    /// The PulseAudio monitor source for the current output device, which is what "system audio"
    /// means on Linux.
    /// </summary>
    /// <remarks>
    /// Derived from the default sink rather than hardcoded - the name differs per machine, and
    /// changes when the user switches output. Returns null when PulseAudio (or PipeWire's
    /// PulseAudio shim) is not reachable, which is what turns the
    /// <see cref="ScreenRecorderCapabilities.SystemAudio"/> flag off.
    /// </remarks>
    public static string? GetDefaultMonitorSource()
    {
        var sink = RunForOutput("pactl", "get-default-sink");

        return String.IsNullOrWhiteSpace(sink) ? null : $"{sink.Trim()}.monitor";
    }


    /// <summary>Whether any PulseAudio-compatible server is reachable.</summary>
    public static bool HasPulseAudio() => RunForOutput("pactl", "info") != null;


    static string? RunForOutput(string fileName, string argument)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(fileName)
            {
                ArgumentList = { argument },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            if (process == null)
                return null;

            var output = process.StandardOutput.ReadToEnd();

            if (!process.WaitForExit(2000))
            {
                try { process.Kill(true); } catch { /* already gone */ }
                return null;
            }

            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
