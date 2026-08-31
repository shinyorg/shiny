using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Shiny.ScreenRecorder;


/// <summary>A live <c>MediaRecorder</c> session.</summary>
class BlazorScreenRecording : AbstractScreenRecording
{
    readonly IJSObjectReference module;
    readonly DotNetObjectReference<BlazorScreenRecording> selfRef;

    int width;
    int height;
    string mimeType = "video/webm";


    public BlazorScreenRecording(
        ScreenRecordingRequest request,
        ScreenRecorderCapabilities capabilities,
        string platformReason,
        IJSObjectReference module,
        ILogger logger
    ) : base(request, capabilities, platformReason, logger)
    {
        this.module = module;
        this.selfRef = DotNetObjectReference.Create(this);
    }


    public async Task Start(CancellationToken ct)
    {
        var options = new StartOptions(
            this.Request.FrameRate,
            this.Request.VideoBitrate,
            this.Request.MaxWidth,
            this.Request.IncludeMicrophone,
            this.Request.IncludeSystemAudio
        );

        StartResult result;
        try
        {
            result = await this.module.InvokeAsync<StartResult>("start", ct, this.selfRef, options).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            // the browser reports a declined picker as NotAllowedError, which is the same shape as
            // a page that is not a secure context or was not responding to a user gesture
            throw ex.Message.Contains("NotAllowed", StringComparison.OrdinalIgnoreCase)
                ? new ScreenRecorderPermissionException("The browser refused screen capture - the user cancelled the picker, the page is not on HTTPS, or Start was not called from a user gesture", ex)
                : new ScreenRecorderException($"The browser could not start screen capture - {ex.Message}", ex);
        }

        this.width = result.Width;
        this.height = result.Height;
        this.mimeType = result.MimeType;

        this.BeginClock();
    }


    /// <summary>The browser's own "Stop sharing" bar ended the capture.</summary>
    [JSInvokable]
    public void OnTrackEnded() => this.OnPlatformStopped(ScreenRecordingFaultReason.RevokedByUser, null);


    protected override async Task OnPause(CancellationToken ct)
        => await this.module.InvokeVoidAsync("pause", ct).ConfigureAwait(false);


    protected override async Task OnResume(CancellationToken ct)
        => await this.module.InvokeVoidAsync("resume", ct).ConfigureAwait(false);


    protected override async Task<ScreenRecordingResult> OnStop(CancellationToken ct)
    {
        var result = await this.module.InvokeAsync<StopResult?>("stop", ct).ConfigureAwait(false)
            ?? throw new ScreenRecorderException("The browser produced no recording");

        return new BlazorScreenRecordingResult(this.module)
        {
            Duration = this.Elapsed,
            ByteSize = result.ByteSize,
            Width = result.Width == 0 ? this.width : result.Width,
            Height = result.Height == 0 ? this.height : result.Height,
            MimeType = String.IsNullOrEmpty(result.MimeType) ? this.mimeType : result.MimeType
        };
    }


    protected override async Task OnCancel(CancellationToken ct)
    {
        try
        {
            await this.module.InvokeVoidAsync("cancel", ct).ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // the circuit or page is gone; the browser has already released the stream
        }
        finally
        {
            this.selfRef.Dispose();
        }
    }


    // there is no file, so the base implementation's delete has nothing to do
    protected override void DeleteOutput() { }


    record StartOptions(int? FrameRate, int? VideoBitrate, int? MaxWidth, bool IncludeMicrophone, bool IncludeSystemAudio);
    record StartResult(int Width, int Height, string MimeType);
    record StopResult(long ByteSize, int Width, int Height, string MimeType);
}
