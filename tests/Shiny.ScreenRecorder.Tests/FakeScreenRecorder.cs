using Microsoft.Extensions.Logging.Abstractions;

namespace Shiny.ScreenRecorder.Tests;


/// <summary>
/// A backend that records what the base classes asked it to do, so the shared state machine can be
/// tested without a screen.
/// </summary>
internal sealed class FakeScreenRecorder(ScreenRecorderCapabilities capabilities) : AbstractScreenRecorder(NullLogger.Instance)
{
    public override ScreenRecorderCapabilities Capabilities { get; } = capabilities;
    protected override string PlatformReason => "the fake test platform";

    public int StartCount { get; private set; }
    public string? LastOutputPath { get; private set; }
    public FakeScreenRecording? Current { get; private set; }

    /// <summary>Set to make the next start fail, the way a declined consent dialog would.</summary>
    public Exception? StartFailure { get; set; }


    protected override Task<AbstractScreenRecording> OnStart(
        ScreenRecordingRequest request,
        string? outputPath,
        CancellationToken ct
    )
    {
        this.StartCount++;
        this.LastOutputPath = outputPath;

        if (this.StartFailure != null)
            throw this.StartFailure;

        this.Current = new FakeScreenRecording(request, this.Capabilities, this.PlatformReason, outputPath);

        return Task.FromResult<AbstractScreenRecording>(this.Current);
    }
}


internal sealed class FakeScreenRecording(
    ScreenRecordingRequest request,
    ScreenRecorderCapabilities capabilities,
    string platformReason,
    string? outputPath
) : AbstractScreenRecording(request, capabilities, platformReason, NullLogger.Instance)
{
    public int PauseCount { get; private set; }
    public int ResumeCount { get; private set; }
    public int StopCount { get; private set; }
    public int CancelCount { get; private set; }

    protected override string? OutputFilePath => outputPath;

    public void Begin() => this.BeginClock();

    public void SimulatePlatformStop(ScreenRecordingFaultReason reason, Exception? exception = null)
        => this.OnPlatformStopped(reason, exception);


    protected override Task OnPause(CancellationToken ct)
    {
        this.PauseCount++;
        return Task.CompletedTask;
    }


    protected override Task OnResume(CancellationToken ct)
    {
        this.ResumeCount++;
        return Task.CompletedTask;
    }


    protected override Task<ScreenRecordingResult> OnStop(CancellationToken ct)
    {
        this.StopCount++;

        return Task.FromResult(new ScreenRecordingResult
        {
            FilePath = outputPath,
            Duration = this.Elapsed,
            ByteSize = 1234,
            Width = 1920,
            Height = 1080,
            MimeType = "video/mp4"
        });
    }


    protected override Task OnCancel(CancellationToken ct)
    {
        this.CancelCount++;
        return Task.CompletedTask;
    }


    // the fake never writes anything, and deleting a path the test owns would be surprising
    protected override void DeleteOutput() { }
}
