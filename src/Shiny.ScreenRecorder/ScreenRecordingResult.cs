namespace Shiny.ScreenRecorder;


/// <summary>
/// A finished recording.
/// </summary>
/// <remarks>
/// <see cref="FilePath"/> is null in the browser, which has no filesystem to write to. Use
/// <see cref="OpenRead"/> when you want the bytes and do not care where they came from - it works
/// on every platform.
/// </remarks>
public record ScreenRecordingResult
{
    /// <summary>
    /// Where the video was written, or null where the platform has no filesystem.
    /// </summary>
    /// <remarks>
    /// On Android this is app-private storage; move or share it before the OS reclaims the cache
    /// directory. On Apple platforms it is inside the app container and is not visible to Photos
    /// until you save it there yourself.
    /// </remarks>
    public string? FilePath { get; init; }

    /// <summary>How long the video runs, excluding any paused span.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Size of the recording in bytes.</summary>
    public required long ByteSize { get; init; }

    /// <summary>Encoded width in pixels.</summary>
    public required int Width { get; init; }

    /// <summary>Encoded height in pixels.</summary>
    public required int Height { get; init; }

    /// <summary>
    /// The container and codec actually produced, e.g. <c>video/mp4</c> or
    /// <c>video/webm;codecs=vp9</c>.
    /// </summary>
    /// <remarks>
    /// Worth reading rather than assuming. Native platforms all produce MP4/H.264, but browsers
    /// disagree - Safari and recent Chrome give MP4, Firefox gives WebM - and an upload endpoint
    /// usually cares.
    /// </remarks>
    public required string MimeType { get; init; }

    /// <summary>
    /// Opens the recording for reading.
    /// </summary>
    /// <remarks>
    /// The caller owns the returned stream and must dispose it. On platforms with a file this
    /// opens <see cref="FilePath"/>; in the browser it streams the recorded blob back out of
    /// JavaScript, which copies it - so read it once rather than repeatedly.
    /// </remarks>
    /// <exception cref="ScreenRecorderException">The recording is no longer available - the file was deleted, or the browser released the blob.</exception>
    public virtual Task<Stream> OpenRead(CancellationToken ct = default)
    {
        if (this.FilePath == null)
            throw new ScreenRecorderException("This recording has no file path and the platform did not provide a reader - this is a bug in the platform implementation");

        if (!File.Exists(this.FilePath))
            throw new ScreenRecorderException($"The recording at '{this.FilePath}' no longer exists");

        return Task.FromResult<Stream>(File.OpenRead(this.FilePath));
    }
}
