using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Shiny.ScreenRecorder;


/// <summary>
/// Shared plumbing for the platform <see cref="IScreenRecording"/> implementations - the
/// pause/stop/cancel state machine, the elapsed clock, the <see cref="ScreenRecordingRequest.MaxDuration"/>
/// watchdog, and the guarantee that a session finishes exactly once no matter how many callers
/// race to end it.
/// </summary>
/// <remarks>
/// <para>Every native backend here can be ended from at least two directions at once: the app
/// calling <see cref="Stop"/>, and the OS revoking the capture. Both paths converge on
/// <see cref="Finish"/>, which is serialised and idempotent, so the encoder is only ever torn down
/// once and the result is only ever computed once.</para>
/// <para><see cref="Elapsed"/> is a <see cref="Stopwatch"/> rather than wall clock arithmetic
/// precisely so pausing is free to implement - stopping the stopwatch is the whole of it, and the
/// duration written into the result then matches the file rather than the session.</para>
/// </remarks>
public abstract class AbstractScreenRecording : IScreenRecording
{
    readonly SemaphoreSlim sync = new(1, 1);
    readonly Stopwatch clock = new();
    readonly ILogger logger;
    readonly CancellationTokenSource watchdogCancel = new();

    ScreenRecordingResult? result;
    bool finished;


    protected AbstractScreenRecording(
        ScreenRecordingRequest request,
        ScreenRecorderCapabilities capabilities,
        string platformReason,
        ILogger logger
    )
    {
        this.Request = request;
        this.Capabilities = capabilities;
        this.PlatformReason = platformReason;
        this.logger = logger;
    }


    /// <summary>The request this session was started from.</summary>
    protected ScreenRecordingRequest Request { get; }

    /// <summary>What the platform can do, carried down so pause can be gated here rather than in every backend.</summary>
    protected ScreenRecorderCapabilities Capabilities { get; }

    /// <summary>The sentence appended to a not-supported message, explaining the platform limit.</summary>
    protected string PlatformReason { get; }

    protected ILogger Logger => this.logger;

    /// <summary>Whether the session has ended, by any route.</summary>
    protected bool IsFinished => Volatile.Read(ref this.finished);

    public TimeSpan Elapsed => this.clock.Elapsed;
    public bool IsPaused { get; private set; }

    public event EventHandler<ScreenRecordingFaultedEventArgs>? Faulted;

    /// <summary>Raised as the session moves, so the owning recorder can mirror it.</summary>
    internal event EventHandler<ScreenRecorderState>? StateTransition;


    /// <summary>
    /// Starts the clock and arms the duration watchdog. The platform calls this once frames are
    /// genuinely flowing, not when the start was merely accepted.
    /// </summary>
    protected void BeginClock()
    {
        this.clock.Start();

        if (this.Request.MaxDuration is { } max)
            _ = this.RunWatchdog(max);
    }


    // deliberately polls Elapsed rather than sleeping for the whole duration: Elapsed excludes
    // paused spans, so a recording paused for a minute should run a minute longer in wall clock
    // than the timer would have allowed
    async Task RunWatchdog(TimeSpan max)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

            while (await timer.WaitForNextTickAsync(this.watchdogCancel.Token).ConfigureAwait(false))
            {
                if (this.Elapsed < max)
                    continue;

                this.logger.MaxDurationReached(max);
                var final = await this.Finish(CancellationToken.None).ConfigureAwait(false);
                this.RaiseFaulted(ScreenRecordingFaultReason.MaxDurationReached, final, null);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // the session ended first, which is the ordinary case
        }
        catch (Exception ex)
        {
            this.logger.WatchdogFailed(ex);
        }
    }


    public async Task Pause(CancellationToken ct = default)
    {
        this.AssertPauseSupported();
        await this.sync.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            this.AssertLive();

            if (this.IsPaused)
                return;

            await this.OnPause(ct).ConfigureAwait(false);
            this.clock.Stop();
            this.IsPaused = true;
            this.StateTransition?.Invoke(this, ScreenRecorderState.Paused);
            this.logger.RecordingPaused(this.Elapsed);
        }
        finally
        {
            this.sync.Release();
        }
    }


    public async Task Resume(CancellationToken ct = default)
    {
        this.AssertPauseSupported();
        await this.sync.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            this.AssertLive();

            if (!this.IsPaused)
                return;

            await this.OnResume(ct).ConfigureAwait(false);
            this.clock.Start();
            this.IsPaused = false;
            this.StateTransition?.Invoke(this, ScreenRecorderState.Recording);
            this.logger.RecordingResumed(this.Elapsed);
        }
        finally
        {
            this.sync.Release();
        }
    }


    public Task<ScreenRecordingResult> Stop(CancellationToken ct = default) => this.Finish(ct);


    public async Task Cancel(CancellationToken ct = default)
    {
        await this.sync.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!this.finished)
            {
                this.finished = true;
                this.clock.Stop();
                await this.watchdogCancel.CancelAsync().ConfigureAwait(false);
                this.StateTransition?.Invoke(this, ScreenRecorderState.Stopping);

                try
                {
                    await this.OnCancel(ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // cancelling is a best-effort teardown - the caller has already said they do
                    // not want the output, so a failure here is logged rather than thrown
                    this.logger.CancelFailed(ex);
                }
                finally
                {
                    this.StateTransition?.Invoke(this, ScreenRecorderState.Idle);
                }
            }

            this.DeleteOutput();
            this.result = null;
        }
        finally
        {
            this.sync.Release();
        }
    }


    /// <summary>
    /// Ends the session exactly once. Both <see cref="Stop"/> and the native "the OS took it away"
    /// callbacks route through here, so whichever arrives first does the teardown and the other
    /// gets the same result back.
    /// </summary>
    protected async Task<ScreenRecordingResult> Finish(CancellationToken ct)
    {
        await this.sync.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (this.finished)
            {
                return this.result
                    ?? throw new ScreenRecorderException("The recording was cancelled - there is no output to return");
            }

            this.finished = true;
            this.clock.Stop();
            await this.watchdogCancel.CancelAsync().ConfigureAwait(false);
            this.StateTransition?.Invoke(this, ScreenRecorderState.Stopping);

            try
            {
                this.result = await this.OnStop(ct).ConfigureAwait(false);
                this.logger.RecordingStopped(this.result.Duration, this.result.ByteSize);
                return this.result;
            }
            finally
            {
                this.StateTransition?.Invoke(this, ScreenRecorderState.Idle);
            }
        }
        finally
        {
            this.sync.Release();
        }
    }


    /// <summary>
    /// Called by a backend when the OS ended the recording. Tears the session down and raises
    /// <see cref="Faulted"/> with whatever was salvaged.
    /// </summary>
    protected async void OnPlatformStopped(ScreenRecordingFaultReason reason, Exception? exception)
    {
        // async void because every caller is a native callback with no Task to hand back to; the
        // body cannot throw, so the process is never at risk from it
        ScreenRecordingResult? salvaged = null;
        try
        {
            this.logger.RecordingFaulted(reason, exception);
            salvaged = await this.Finish(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.FinishAfterFaultFailed(ex);
        }

        this.RaiseFaulted(reason, salvaged, exception);
    }


    void RaiseFaulted(ScreenRecordingFaultReason reason, ScreenRecordingResult? salvaged, Exception? exception)
    {
        try
        {
            this.Faulted?.Invoke(this, new ScreenRecordingFaultedEventArgs(reason, salvaged, exception));
        }
        catch (Exception ex)
        {
            this.logger.FaultHandlerThrew(ex);
        }
    }


    /// <summary>Removes the partial or finished output file, where the platform wrote one.</summary>
    protected virtual void DeleteOutput()
    {
        var path = this.result?.FilePath ?? this.OutputFilePath;
        if (path == null || !File.Exists(path))
            return;

        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            this.logger.DeleteOutputFailed(path, ex);
        }
    }


    /// <summary>
    /// Where this session is writing, so <see cref="Cancel"/> can clean up a partial file before a
    /// result exists. Null on platforms with no filesystem.
    /// </summary>
    protected virtual string? OutputFilePath => null;

    protected abstract Task OnPause(CancellationToken ct);
    protected abstract Task OnResume(CancellationToken ct);
    protected abstract Task<ScreenRecordingResult> OnStop(CancellationToken ct);
    protected abstract Task OnCancel(CancellationToken ct);


    void AssertPauseSupported()
    {
        if (!this.Capabilities.HasFlag(ScreenRecorderCapabilities.PauseResume))
            throw ScreenRecorderNotSupportedException.For(ScreenRecorderCapabilities.PauseResume, this.PlatformReason);
    }


    void AssertLive() => ObjectDisposedException.ThrowIf(this.finished, this);


    public async ValueTask DisposeAsync()
    {
        // disposing a session that is still running means the caller does not want the output, so
        // it is cancelled and the partial file removed. A session that already finished must be
        // left alone - `await using` around a Stop is the ordinary way to use this, and deleting
        // the recording the caller just asked for would be an unpleasant surprise.
        if (!this.IsFinished)
            await this.Cancel(CancellationToken.None).ConfigureAwait(false);

        this.watchdogCancel.Dispose();
        this.sync.Dispose();
        GC.SuppressFinalize(this);
    }
}
