using Microsoft.JSInterop;

namespace Shiny.ScreenRecorder;


/// <summary>
/// A finished browser recording, which lives as a Blob rather than a file.
/// </summary>
/// <remarks>
/// <see cref="ScreenRecordingResult.FilePath"/> is null here - there is no filesystem to write to -
/// so <see cref="OpenRead"/> streams the Blob out of JavaScript instead. Everything else about the
/// result is the same shape as on the native platforms, which is what lets calling code stay
/// portable.
/// </remarks>
public record BlazorScreenRecordingResult : ScreenRecordingResult
{
    readonly IJSObjectReference module;

    internal BlazorScreenRecordingResult(IJSObjectReference module) => this.module = module;


    /// <summary>
    /// Streams the recording out of the browser.
    /// </summary>
    /// <remarks>
    /// The bytes are copied across the interop boundary, so read the stream once and hold what you
    /// need rather than calling this repeatedly - a few minutes of screen capture is tens of
    /// megabytes.
    /// </remarks>
    public override async Task<Stream> OpenRead(CancellationToken ct = default)
    {
        try
        {
            var reference = await this.module
                .InvokeAsync<IJSStreamReference>("read", ct)
                .ConfigureAwait(false);

            // the default cap is 512KB, which no screen recording fits inside; the real size is
            // already known, and the slack covers the container overhead the Blob reports
            return await reference
                .OpenReadStreamAsync(this.ByteSize + 1024, ct)
                .ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw new ScreenRecorderException("The browser no longer holds this recording - it is released when a new one starts or the page reloads", ex);
        }
    }
}
