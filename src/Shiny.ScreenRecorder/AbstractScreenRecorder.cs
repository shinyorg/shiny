using Microsoft.Extensions.Logging;

namespace Shiny.ScreenRecorder;


/// <summary>
/// Shared plumbing for the platform <see cref="IScreenRecorder"/> implementations - request
/// validation against <see cref="Capabilities"/>, the one-recording-at-a-time guard, output path
/// resolution, and mirroring the live session's state onto <see cref="StateChanged"/>.
/// </summary>
/// <remarks>
/// <para>Validation happens here, before any native call, so a request asking for something the
/// platform cannot do fails with a message naming the capability rather than with whatever the OS
/// says several layers down - which on most of these platforms is nothing at all, the setting just
/// gets ignored.</para>
/// <para>The single-session guard is not a design preference. MediaProjection, ReplayKit,
/// ScreenCaptureKit and Windows.Graphics.Capture all refuse a second concurrent capture; failing
/// here is simply earlier and clearer.</para>
/// </remarks>
public abstract class AbstractScreenRecorder(ILogger logger) : IScreenRecorder
{
    readonly SemaphoreSlim startLock = new(1, 1);
    AbstractScreenRecording? current;
    ScreenRecorderState state;


    public abstract ScreenRecorderCapabilities Capabilities { get; }

    /// <summary>
    /// The sentence appended to every not-supported message from this backend, explaining why the
    /// platform cannot do the thing.
    /// </summary>
    protected abstract string PlatformReason { get; }

    protected ILogger Logger => logger;


    public ScreenRecorderState State
    {
        get => this.state;
        private set
        {
            if (this.state == value)
                return;

            this.state = value;
            try
            {
                this.StateChanged?.Invoke(this, value);
            }
            catch (Exception ex)
            {
                logger.StateHandlerThrew(ex);
            }
        }
    }

    public event EventHandler<ScreenRecorderState>? StateChanged;


    public virtual Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(
            this.Capabilities.HasFlag(ScreenRecorderCapabilities.Recording)
                ? AccessState.Available
                : AccessState.NotSupported
        );
    }


    public virtual Task<IReadOnlyList<CaptureTarget>> GetTargets(CancellationToken ct = default)
        => throw ScreenRecorderNotSupportedException.For(ScreenRecorderCapabilities.DisplaySelection, this.PlatformReason);


    public async Task<IScreenRecording> Start(ScreenRecordingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.AssertValid(this.Capabilities, this.PlatformReason);

        // held for the whole start, not just the check: two callers racing here would otherwise
        // both see a null current and both reach the native start
        await this.startLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (this.current != null)
                throw new ScreenRecorderException("A recording is already in progress - stop it before starting another");

            this.State = ScreenRecorderState.Starting;
            var outputPath = this.ResolveOutputPath(request);
            logger.RecordingStarting(outputPath, request.Target?.Name);

            AbstractScreenRecording recording;
            try
            {
                recording = await this.OnStart(request, outputPath, ct).ConfigureAwait(false);
            }
            catch
            {
                this.State = ScreenRecorderState.Idle;
                throw;
            }

            this.current = recording;
            recording.StateTransition += this.OnSessionStateTransition;
            this.State = ScreenRecorderState.Recording;

            return recording;
        }
        finally
        {
            this.startLock.Release();
        }
    }


    void OnSessionStateTransition(object? sender, ScreenRecorderState transition)
    {
        this.State = transition;

        if (transition != ScreenRecorderState.Idle)
            return;

        if (sender is AbstractScreenRecording finished)
            finished.StateTransition -= this.OnSessionStateTransition;

        // only clear when the session that ended is the one we are tracking - a late Idle from a
        // previous session must not release the guard on a newer one
        if (ReferenceEquals(this.current, sender))
            this.current = null;
    }


    /// <summary>
    /// Decides where the recording is written. Null is returned only on platforms with no
    /// filesystem, which override this.
    /// </summary>
    protected virtual string? ResolveOutputPath(ScreenRecordingRequest request)
    {
        if (!String.IsNullOrWhiteSpace(request.OutputPath))
        {
            var directory = Path.GetDirectoryName(request.OutputPath);
            if (!String.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            return request.OutputPath;
        }

        var root = this.DefaultOutputDirectory;
        Directory.CreateDirectory(root);

        return Path.Combine(root, $"screen-{DateTime.Now:yyyyMMdd-HHmmss}{this.DefaultFileExtension}");
    }


    /// <summary>Where generated output goes when the request did not name a path.</summary>
    /// <remarks>
    /// Defaults to the process temp directory. The mobile backends override this with the app's
    /// own cache directory, which is what the OS actually lets them write to.
    /// </remarks>
    protected virtual string DefaultOutputDirectory => Path.Combine(Path.GetTempPath(), "shiny-screen-recorder");

    /// <summary>The container extension this backend produces, including the dot.</summary>
    protected virtual string DefaultFileExtension => ".mp4";


    /// <summary>
    /// Starts the native capture. Called with the request already validated and the output path
    /// already resolved, and must not return until frames are genuinely being written.
    /// </summary>
    protected abstract Task<AbstractScreenRecording> OnStart(
        ScreenRecordingRequest request,
        string? outputPath,
        CancellationToken ct
    );


    /// <summary>Throws when the given capability is missing, naming it and the platform reason.</summary>
    protected void AssertCapability(ScreenRecorderCapabilities capability)
    {
        if (!this.Capabilities.HasFlag(capability))
            throw ScreenRecorderNotSupportedException.For(capability, this.PlatformReason);
    }
}
