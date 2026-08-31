using Xunit;

namespace Shiny.ScreenRecorder.Tests;


/// <summary>
/// The lifecycle every backend inherits.
/// </summary>
/// <remarks>
/// Two things here are load-bearing and neither is obvious from the interface. First, a session can
/// be ended from two directions at once - the app calling Stop, and the OS revoking the capture -
/// and the encoder must only be torn down once. Second, the recorder has to notice when its session
/// ends by itself, or the one-recording-at-a-time guard would refuse every subsequent start for the
/// life of the app.
/// </remarks>
public class ScreenRecorderStateTests
{
    const ScreenRecorderCapabilities Full =
        ScreenRecorderCapabilities.Recording | ScreenRecorderCapabilities.PauseResume;


    [Fact]
    public async Task StartMovesToRecording()
    {
        var recorder = new FakeScreenRecorder(Full);
        Assert.Equal(ScreenRecorderState.Idle, recorder.State);

        await recorder.Start(new ScreenRecordingRequest());

        Assert.Equal(ScreenRecorderState.Recording, recorder.State);
    }


    [Fact]
    public async Task StateChangesAreRaised()
    {
        var recorder = new FakeScreenRecorder(Full);
        var seen = new List<ScreenRecorderState>();
        recorder.StateChanged += (_, s) => seen.Add(s);

        var recording = await recorder.Start(new ScreenRecordingRequest());
        await recording.Pause();
        await recording.Resume();
        await recording.Stop();

        Assert.Equal(
            new[]
            {
                ScreenRecorderState.Starting,
                ScreenRecorderState.Recording,
                ScreenRecorderState.Paused,
                ScreenRecorderState.Recording,
                ScreenRecorderState.Stopping,
                ScreenRecorderState.Idle
            },
            seen
        );
    }


    [Fact]
    public async Task OnlyOneRecordingAtATime()
    {
        var recorder = new FakeScreenRecorder(Full);
        await recorder.Start(new ScreenRecordingRequest());

        await Assert.ThrowsAsync<ScreenRecorderException>(() => recorder.Start(new ScreenRecordingRequest()));
    }


    [Fact]
    public async Task StoppingReleasesTheGuard()
    {
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest());
        await recording.Stop();

        await recorder.Start(new ScreenRecordingRequest());

        Assert.Equal(2, recorder.StartCount);
    }


    [Fact]
    public async Task APlatformStopReleasesTheGuard()
    {
        // the case that matters: the user hit the OS "stop sharing" button rather than the app
        // calling Stop. If the recorder does not notice, every later start fails forever.
        var recorder = new FakeScreenRecorder(Full);
        await recorder.Start(new ScreenRecordingRequest());

        recorder.Current!.SimulatePlatformStop(ScreenRecordingFaultReason.RevokedByUser);
        await WaitFor(() => recorder.State == ScreenRecorderState.Idle);

        await recorder.Start(new ScreenRecordingRequest());

        Assert.Equal(2, recorder.StartCount);
    }


    [Fact]
    public async Task AFailedStartLeavesTheRecorderIdle()
    {
        var recorder = new FakeScreenRecorder(Full)
        {
            StartFailure = new ScreenRecorderPermissionException("declined")
        };

        await Assert.ThrowsAsync<ScreenRecorderPermissionException>(() => recorder.Start(new ScreenRecordingRequest()));

        Assert.Equal(ScreenRecorderState.Idle, recorder.State);

        recorder.StartFailure = null;
        await recorder.Start(new ScreenRecordingRequest());
    }


    [Fact]
    public async Task StoppingTwiceTearsDownOnce()
    {
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest());

        var first = await recording.Stop();
        var second = await recording.Stop();

        Assert.Equal(1, recorder.Current!.StopCount);
        Assert.Equal(first, second);
    }


    [Fact]
    public async Task APlatformStopThenStopStillTearsDownOnce()
    {
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest());

        recorder.Current!.SimulatePlatformStop(ScreenRecordingFaultReason.InterruptedBySystem);
        await WaitFor(() => recorder.State == ScreenRecorderState.Idle);

        await recording.Stop();

        Assert.Equal(1, recorder.Current.StopCount);
    }


    [Fact]
    public async Task FaultedCarriesTheSalvagedResult()
    {
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest());

        ScreenRecordingFaultedEventArgs? faulted = null;
        recording.Faulted += (_, e) => faulted = e;

        recorder.Current!.SimulatePlatformStop(ScreenRecordingFaultReason.RevokedByUser);
        await WaitFor(() => faulted != null);

        Assert.Equal(ScreenRecordingFaultReason.RevokedByUser, faulted!.Reason);
        Assert.NotNull(faulted.Result);
        Assert.Equal(1234, faulted.Result!.ByteSize);
    }


    [Fact]
    public async Task PauseIsIdempotent()
    {
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest());

        await recording.Pause();
        await recording.Pause();

        Assert.Equal(1, recorder.Current!.PauseCount);
        Assert.True(recording.IsPaused);
    }


    [Fact]
    public async Task ResumeWithoutPauseDoesNothing()
    {
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest());

        await recording.Resume();

        Assert.Equal(0, recorder.Current!.ResumeCount);
    }


    [Fact]
    public async Task PauseThrowsWhereItIsNotSupported()
    {
        var recorder = new FakeScreenRecorder(ScreenRecorderCapabilities.Recording);
        var recording = await recorder.Start(new ScreenRecordingRequest());

        await Assert.ThrowsAsync<ScreenRecorderNotSupportedException>(() => recording.Pause());
    }


    [Fact]
    public async Task ElapsedExcludesThePausedSpan()
    {
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest());
        recorder.Current!.Begin();

        await Task.Delay(60);
        await recording.Pause();
        var atPause = recording.Elapsed;

        await Task.Delay(150);
        var afterWaiting = recording.Elapsed;

        Assert.Equal(atPause, afterWaiting);
    }


    [Fact]
    public async Task DisposingWithoutStoppingCancels()
    {
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest());

        await recording.DisposeAsync();

        Assert.Equal(1, recorder.Current!.CancelCount);
        Assert.Equal(0, recorder.Current.StopCount);
        Assert.Equal(ScreenRecorderState.Idle, recorder.State);
    }


    [Fact]
    public async Task DisposingAfterStoppingKeepsTheRecording()
    {
        // `await using` around a Stop is the ordinary way to use this, and it must not throw away
        // the file the caller just asked for
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest());

        await recording.Stop();
        await recording.DisposeAsync();

        Assert.Equal(1, recorder.Current!.StopCount);
        Assert.Equal(0, recorder.Current.CancelCount);
    }


    [Fact]
    public async Task StopAfterCancelReportsThereIsNoOutput()
    {
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest());

        await recording.Cancel();

        await Assert.ThrowsAsync<ScreenRecorderException>(() => recording.Stop());
    }


    [Fact]
    public async Task MaxDurationStopsTheRecordingAndReportsIt()
    {
        var recorder = new FakeScreenRecorder(Full);
        var recording = await recorder.Start(new ScreenRecordingRequest
        {
            MaxDuration = TimeSpan.FromMilliseconds(300)
        });
        recorder.Current!.Begin();

        ScreenRecordingFaultedEventArgs? faulted = null;
        recording.Faulted += (_, e) => faulted = e;

        await WaitFor(() => faulted != null, TimeSpan.FromSeconds(5));

        Assert.Equal(ScreenRecordingFaultReason.MaxDurationReached, faulted!.Reason);
        Assert.NotNull(faulted.Result);
        Assert.Equal(ScreenRecorderState.Idle, recorder.State);
    }


    [Fact]
    public async Task RequestOutputPathIsPassedThrough()
    {
        var recorder = new FakeScreenRecorder(Full);
        var path = Path.Combine(Path.GetTempPath(), "shiny-tests", "explicit.mp4");

        await recorder.Start(new ScreenRecordingRequest { OutputPath = path });

        Assert.Equal(path, recorder.LastOutputPath);
        Assert.True(Directory.Exists(Path.GetDirectoryName(path)));
    }


    [Fact]
    public async Task AGeneratedOutputPathIsUsedWhenNoneIsGiven()
    {
        var recorder = new FakeScreenRecorder(Full);

        await recorder.Start(new ScreenRecordingRequest());

        Assert.NotNull(recorder.LastOutputPath);
        Assert.EndsWith(".mp4", recorder.LastOutputPath);
    }


    static async Task WaitFor(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(2));

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(10);
        }

        Assert.Fail("The expected state was not reached in time");
    }
}
