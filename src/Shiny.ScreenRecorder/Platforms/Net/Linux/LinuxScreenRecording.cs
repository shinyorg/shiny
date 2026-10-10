using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Shiny.ScreenRecorder.Encoders;
using Shiny.ScreenRecorder.Infrastructure;
using Shiny.ScreenRecorder.Portal;

namespace Shiny.ScreenRecorder;


/// <summary>
/// A recording driven by an external encoder process.
/// </summary>
/// <remarks>
/// <para><b>Stopping means SIGINT, never Kill.</b> Both gst-launch (with <c>-e</c>) and ffmpeg
/// treat SIGINT as "finish up": they flush the encoder and write the MP4's moov atom. .NET's
/// <c>Process.Kill</c> sends SIGKILL, which leaves a file with no index that no player will open -
/// so the signal goes through libc directly, and SIGKILL is only ever the last resort after the
/// graceful path has timed out.</para>
/// <para>There is no pause. Neither encoder can suspend a running pipeline and resume it into the
/// same file, and SIGSTOP would freeze the process with its buffers full rather than closing the
/// gap in the timeline.</para>
/// </remarks>
partial class LinuxScreenRecording : AbstractScreenRecording
{
    const int SIGINT = 2;

    readonly EncoderCommand command;
    readonly VideoDimensions dimensions;
    readonly string outputPath;
    readonly ScreenCastPortal? portal;
    readonly StringBuilder errorOutput = new();

    Process? process;


    public LinuxScreenRecording(
        ScreenRecordingRequest request,
        ScreenRecorderCapabilities capabilities,
        string platformReason,
        EncoderCommand command,
        VideoDimensions dimensions,
        string outputPath,
        ScreenCastPortal? portal,
        ILogger logger
    ) : base(request, capabilities, platformReason, logger)
    {
        this.command = command;
        this.dimensions = dimensions;
        this.outputPath = outputPath;
        this.portal = portal;
    }


    [LibraryImport("libc", SetLastError = true)]
    private static partial int kill(int pid, int signal);


    protected override string? OutputFilePath => this.outputPath;


    public void Start()
    {
        if (File.Exists(this.outputPath))
            File.Delete(this.outputPath);

        var info = new ProcessStartInfo(this.command.FileName)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in this.command.Arguments)
            info.ArgumentList.Add(argument);

        this.process = Process.Start(info)
            ?? throw new ScreenRecorderException($"Could not start the encoder - {this.command.FileName}");

        this.process.EnableRaisingEvents = true;
        this.process.Exited += this.OnProcessExited;

        // both encoders write everything to stderr; keeping it means an unusable recording can say
        // why instead of just reporting a missing file
        this.process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                lock (this.errorOutput)
                    this.errorOutput.AppendLine(e.Data);
        };
        this.process.BeginErrorReadLine();
        this.process.BeginOutputReadLine();

        // an encoder that fails on its arguments dies within milliseconds, and reporting that here
        // is far clearer than returning a session that produces an empty file
        if (this.process.WaitForExit(500))
            throw new ScreenRecorderException($"The encoder exited immediately - {this.ReadErrorOutput()}");

        this.BeginClock();
    }


    void OnProcessExited(object? sender, EventArgs e)
    {
        if (this.IsFinished)
            return;

        this.OnPlatformStopped(
            ScreenRecordingFaultReason.EncoderFailed,
            new ScreenRecorderException($"The encoder stopped unexpectedly - {this.ReadErrorOutput()}")
        );
    }


    protected override Task OnPause(CancellationToken ct)
        => throw ScreenRecorderNotSupportedException.For(ScreenRecorderCapabilities.PauseResume, this.PlatformReason);

    protected override Task OnResume(CancellationToken ct)
        => throw ScreenRecorderNotSupportedException.For(ScreenRecorderCapabilities.PauseResume, this.PlatformReason);


    protected override async Task<ScreenRecordingResult> OnStop(CancellationToken ct)
    {
        var finalised = await this.StopEncoder().ConfigureAwait(false);
        await this.ClosePortal().ConfigureAwait(false);

        var info = new FileInfo(this.outputPath);
        if (!info.Exists || info.Length == 0)
            throw new ScreenRecorderException($"The encoder produced no file - {this.ReadErrorOutput()}");

        if (!finalised)
            this.Logger.EncoderDidNotFinalise();

        return new ScreenRecordingResult
        {
            FilePath = this.outputPath,
            Duration = this.Elapsed,
            ByteSize = info.Length,
            Width = this.dimensions.Width,
            Height = this.dimensions.Height,
            MimeType = "video/mp4"
        };
    }


    protected override async Task OnCancel(CancellationToken ct)
    {
        await this.StopEncoder().ConfigureAwait(false);
        await this.ClosePortal().ConfigureAwait(false);
    }


    /// <summary>Signals the encoder to finish and waits for it. False means it had to be killed.</summary>
    async Task<bool> StopEncoder()
    {
        var running = this.process;
        if (running == null)
            return false;

        running.Exited -= this.OnProcessExited;

        if (running.HasExited)
            return running.ExitCode == 0;

        if (kill(running.Id, SIGINT) != 0)
            this.Logger.SignalFailed(Marshal.GetLastPInvokeError());

        try
        {
            // flushing and writing the index on a long recording is not instant
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await running.WaitForExitAsync(timeout.Token).ConfigureAwait(false);

            return true;
        }
        catch (OperationCanceledException)
        {
            // the graceful path is exhausted; the file will be missing its index, which OnStop
            // reports rather than passing off as a good recording
            try { running.Kill(true); } catch { /* already gone */ }

            return false;
        }
        finally
        {
            running.Dispose();
            this.process = null;
        }
    }


    async Task ClosePortal()
    {
        if (this.portal != null)
            await this.portal.DisposeAsync().ConfigureAwait(false);
    }


    string ReadErrorOutput()
    {
        lock (this.errorOutput)
        {
            var text = this.errorOutput.ToString().Trim();

            return text.Length == 0
                ? "the encoder wrote nothing to stderr"
                : text.Length > 500 ? text[^500..] : text;
        }
    }
}
