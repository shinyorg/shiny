namespace Shiny.ScreenRecorder.Infrastructure;


/// <summary>
/// Turns a native capture size and a request into the dimensions and bitrate the encoder is
/// actually given.
/// </summary>
/// <remarks>
/// Shared by every backend because they all have the same two problems: H.264 cannot encode odd
/// dimensions, and none of the platforms pick a sensible default bitrate for a screen (they assume
/// camera footage, which has far more motion and far less fine text).
/// </remarks>
internal readonly record struct VideoDimensions(int Width, int Height, int Bitrate, int FrameRate)
{
    public const int DefaultFrameRate = 30;


    /// <summary>
    /// Scales <paramref name="nativeWidth"/> x <paramref name="nativeHeight"/> down to the
    /// request's <see cref="ScreenRecordingRequest.MaxWidth"/>, rounds both sides to even, and
    /// picks a bitrate when the request did not.
    /// </summary>
    public static VideoDimensions From(ScreenRecordingRequest request, int nativeWidth, int nativeHeight)
    {
        if (nativeWidth <= 0 || nativeHeight <= 0)
            throw new ScreenRecorderException($"The capture target reported a nonsensical size ({nativeWidth}x{nativeHeight})");

        var width = nativeWidth;
        var height = nativeHeight;

        if (request.MaxWidth is { } max && max < nativeWidth)
        {
            width = max;

            // scale the height off the *unrounded* ratio - rounding the width first and then
            // deriving the height compounds the error on tall aspect ratios
            height = (int)Math.Round(nativeHeight * ((double)max / nativeWidth));
        }

        width = MakeEven(width);
        height = MakeEven(height);

        var frameRate = request.FrameRate ?? DefaultFrameRate;
        var bitrate = request.VideoBitrate ?? EstimateBitrate(width, height, frameRate);

        return new VideoDimensions(width, height, bitrate, frameRate);
    }


    // H.264 macroblocks are 16x16 and every encoder here rejects odd dimensions outright; 2 is the
    // coarsest rounding that satisfies all of them, and clamping at 2 keeps a degenerate 1px
    // target from producing a zero
    static int MakeEven(int value) => Math.Max(2, value - (value % 2));


    /// <summary>
    /// Roughly 0.1 bits per pixel per frame, clamped to a range that keeps text legible without
    /// producing gigabyte files.
    /// </summary>
    /// <remarks>
    /// Screen content is mostly static with sharp edges, so it compresses far better than camera
    /// footage - but the sharp edges are exactly what a low bitrate destroys, and unreadable text
    /// makes a screen recording worthless. The floor matters more than the ceiling here.
    /// </remarks>
    static int EstimateBitrate(int width, int height, int frameRate)
    {
        var estimate = (long)(width * height * frameRate * 0.1);

        return (int)Math.Clamp(estimate, 1_500_000L, 40_000_000L);
    }
}
