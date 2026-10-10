using System.Globalization;
using Shiny.ScreenRecorder.Infrastructure;

namespace Shiny.ScreenRecorder.Encoders;


/// <summary>An external encoder invocation, ready to hand to <see cref="System.Diagnostics.Process"/>.</summary>
internal sealed record EncoderCommand(string FileName, IReadOnlyList<string> Arguments)
{
    /// <summary>The command as a shell would show it. For logs and error messages only.</summary>
    public string Display => $"{this.FileName} {String.Join(' ', this.Arguments)}";
}


/// <summary>
/// Builds the GStreamer and FFmpeg command lines.
/// </summary>
/// <remarks>
/// <para>Pure string construction with no I/O, which is deliberate - the rest of the Linux backend
/// cannot be tested without a compositor, and this is where the mistakes that actually break a
/// recording live: a missing <c>-e</c> that leaves the MP4 without an index, an odd width the
/// encoder rejects, a caps filter in the wrong place.</para>
/// <para>Arguments are returned as a list rather than a joined string so nothing has to be quoted
/// or escaped - <c>ProcessStartInfo.ArgumentList</c> passes each through untouched, which matters
/// because output paths contain spaces.</para>
/// </remarks>
internal static class EncoderCommandBuilder
{
    /// <summary>
    /// A GStreamer pipeline reading the portal's PipeWire node.
    /// </summary>
    /// <remarks>
    /// <para><c>-e</c> is load-bearing: it makes gst-launch send an end-of-stream on SIGINT so
    /// <c>mp4mux</c> writes the moov atom. Without it the file has no index and no player will
    /// open it.</para>
    /// <para><c>tune=zerolatency</c> stops x264 buffering frames it is waiting to reorder, which on
    /// a screen that does not change would otherwise hold the last second of the recording
    /// hostage indefinitely.</para>
    /// </remarks>
    public static EncoderCommand GStreamer(
        uint nodeId,
        VideoDimensions dimensions,
        string outputPath,
        string? monitorSource,
        bool includeMicrophone
    )
    {
        var args = new List<string> { "-e" };

        args.Add($"pipewiresrc");
        args.Add($"path={nodeId.ToString(CultureInfo.InvariantCulture)}");
        args.Add("!");
        args.Add("videoconvert");
        args.Add("!");
        args.Add("videoscale");
        args.Add("!");
        args.Add($"video/x-raw,width={dimensions.Width},height={dimensions.Height},framerate={dimensions.FrameRate}/1");
        args.Add("!");

        // x264enc takes kbit/s, not bit/s - passing bits produces a file hundreds of times larger
        // than asked for
        args.Add("x264enc");
        args.Add($"bitrate={dimensions.Bitrate / 1000}");
        args.Add("speed-preset=veryfast");
        args.Add("tune=zerolatency");
        args.Add($"key-int-max={dimensions.FrameRate * 2}");
        args.Add("!");
        args.Add("h264parse");
        args.Add("!");
        args.Add("mp4mux");
        args.Add("name=mux");
        args.Add("!");
        args.Add("filesink");
        args.Add($"location={outputPath}");

        AddGStreamerAudio(args, monitorSource, includeMicrophone);

        return new EncoderCommand("gst-launch-1.0", args);
    }


    // each audio source is its own branch terminating at the named mux; two branches means two
    // audio tracks, so when both are requested they are mixed into one with audiomixer instead
    static void AddGStreamerAudio(List<string> args, string? monitorSource, bool includeMicrophone)
    {
        var sources = new List<string>();

        if (monitorSource != null)
            sources.Add(monitorSource);

        if (includeMicrophone)
            sources.Add(String.Empty);

        if (sources.Count == 0)
            return;

        if (sources.Count == 1)
        {
            AddPulseBranch(args, sources[0], "mux.");
            return;
        }

        args.Add("audiomixer");
        args.Add("name=amix");
        args.Add("!");
        args.Add("audioconvert");
        args.Add("!");
        args.Add("avenc_aac");
        args.Add("bitrate=128000");
        args.Add("!");
        args.Add("queue");
        args.Add("!");
        args.Add("mux.");

        foreach (var source in sources)
            AddPulseBranch(args, source, "amix.", encode: false);
    }


    static void AddPulseBranch(List<string> args, string device, string sink, bool encode = true)
    {
        args.Add("pulsesrc");

        if (!String.IsNullOrEmpty(device))
            args.Add($"device={device}");

        args.Add("!");
        args.Add("audioconvert");
        args.Add("!");
        args.Add("audioresample");
        args.Add("!");

        if (encode)
        {
            args.Add("avenc_aac");
            args.Add("bitrate=128000");
            args.Add("!");
            args.Add("queue");
            args.Add("!");
        }

        args.Add(sink);
    }


    /// <summary>
    /// An FFmpeg <c>x11grab</c> invocation, for X11 sessions with no working portal.
    /// </summary>
    /// <remarks>
    /// Captures the display named by <c>DISPLAY</c> wholesale. There is no picker and no consent
    /// step, which is exactly why this is the fallback and not the default.
    /// </remarks>
    public static EncoderCommand FfmpegX11(
        string display,
        VideoDimensions dimensions,
        string outputPath,
        bool showCursor,
        bool includeAudio
    )
    {
        var args = new List<string>
        {
            "-hide_banner",
            "-loglevel", "error",
            "-y",
            "-f", "x11grab",
            "-framerate", dimensions.FrameRate.ToString(CultureInfo.InvariantCulture),
            "-draw_mouse", showCursor ? "1" : "0",
            "-i", display
        };

        if (includeAudio)
        {
            args.Add("-f");
            args.Add("pulse");
            args.Add("-i");
            args.Add("default");
        }

        args.Add("-c:v");
        args.Add("libx264");
        args.Add("-preset");
        args.Add("veryfast");
        args.Add("-b:v");
        args.Add(dimensions.Bitrate.ToString(CultureInfo.InvariantCulture));

        // x11grab hands over whatever size the display is; the scale filter is what honours MaxWidth
        args.Add("-vf");
        args.Add($"scale={dimensions.Width}:{dimensions.Height}");

        // yuv420p rather than the x11grab-native bgr0, because nothing outside FFmpeg plays 4:4:4
        args.Add("-pix_fmt");
        args.Add("yuv420p");

        if (includeAudio)
        {
            args.Add("-c:a");
            args.Add("aac");
            args.Add("-b:a");
            args.Add("128k");
        }

        args.Add("-movflags");
        args.Add("+faststart");
        args.Add(outputPath);

        return new EncoderCommand("ffmpeg", args);
    }
}
