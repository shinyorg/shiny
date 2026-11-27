using Android.Media;
using Android.Views;
using Microsoft.Extensions.Logging;
using Shiny.ScreenRecorder.Infrastructure;

namespace Shiny.ScreenRecorder;


/// <summary>
/// An H.264 encoder fed by a <see cref="Surface"/>, which is what the VirtualDisplay draws into.
/// </summary>
/// <remarks>
/// <para>Surface input means no frame ever passes through managed code - the compositor writes
/// straight into the encoder. The drain loop only ever moves already-encoded bytes to the muxer,
/// which is why a full-resolution screen recording costs so little CPU.</para>
/// <para>Pause is a timestamp subtraction. The compositor keeps producing frames into the surface
/// while paused and there is no way to stop it, so the encoded output is dropped and the span it
/// covered is discounted from every later sample. The alternative - letting the frames through -
/// would leave a frozen stretch in the middle of the file.</para>
/// </remarks>
internal sealed class VideoSurfaceEncoder : IDisposable
{
    const string MimeType = "video/avc";

    readonly MediaMuxerSink muxer;
    readonly ILogger logger;
    readonly MediaCodec codec;
    readonly Thread drainThread;
    readonly object pauseGate = new();

    readonly PauseTimeline timeline = new();

    int trackIndex = -1;
    volatile bool draining = true;


    public VideoSurfaceEncoder(VideoDimensions dimensions, MediaMuxerSink muxer, ILogger logger)
    {
        this.muxer = muxer;
        this.logger = logger;

        var format = MediaFormat.CreateVideoFormat(MimeType, dimensions.Width, dimensions.Height)!;
        format.SetInteger(MediaFormat.KeyColorFormat, (int)MediaCodecCapabilities.Formatsurface);
        format.SetInteger(MediaFormat.KeyBitRate, dimensions.Bitrate);
        format.SetInteger(MediaFormat.KeyFrameRate, dimensions.FrameRate);

        // a keyframe every 2s; screen content is mostly static, so more would spend the bitrate on
        // I-frames that change nothing, and fewer would make seeking unusable
        format.SetInteger(MediaFormat.KeyIFrameInterval, 2);

        this.codec = MediaCodec.CreateEncoderByType(MimeType)!;
        this.codec.Configure(format, null, null, MediaCodecConfigFlags.Encode);

        // must be created after Configure and before Start
        this.InputSurface = this.codec.CreateInputSurface()!;
        this.codec.Start();

        this.drainThread = new Thread(this.Drain) { IsBackground = true, Name = "shiny-screen-video" };
        this.drainThread.Start();
    }


    /// <summary>The surface the VirtualDisplay renders into.</summary>
    public Surface InputSurface { get; }

    /// <summary>
    /// How much video has been written, from the first frame to the last with paused spans removed.
    /// </summary>
    /// <remarks>
    /// The encoder's timestamps come from the surface, which is the system's monotonic clock - the
    /// last one on its own is device uptime, not a recording length.
    /// </remarks>
    public TimeSpan Duration
    {
        get
        {
            lock (this.pauseGate)
                return TimeSpan.FromMilliseconds(this.timeline.Duration / 1000d);
        }
    }


    public void Pause()
    {
        lock (this.pauseGate)
            this.timeline.Pause();
    }


    public void Resume()
    {
        lock (this.pauseGate)
            this.timeline.Resume();
    }


    void Drain()
    {
        var info = new MediaCodec.BufferInfo();

        try
        {
            while (this.draining)
            {
                var index = this.codec.DequeueOutputBuffer(info, 10_000);

                if (index == (int)MediaCodecInfoState.TryAgainLater)
                    continue;

                if (index == (int)MediaCodecInfoState.OutputFormatChanged)
                {
                    this.trackIndex = this.muxer.AddTrack(this.codec.OutputFormat!);
                    continue;
                }

                if (index < 0)
                    continue;

                using var buffer = this.codec.GetOutputBuffer(index);

                // the codec-config buffer carries SPS/PPS, which MediaMuxer already took from the
                // output format - writing it as a sample corrupts the track
                var isConfig = (info.Flags & MediaCodecBufferFlags.CodecConfig) != 0;

                if (!isConfig && buffer != null && info.Size > 0)
                    this.WriteSample(buffer, info);

                this.codec.ReleaseOutputBuffer(index, false);

                if ((info.Flags & MediaCodecBufferFlags.EndOfStream) != 0)
                    break;
            }
        }
        catch (Java.Lang.IllegalStateException)
        {
            // the codec was released out from under the loop by Stop(); an ordinary shutdown
        }
        catch (Exception ex)
        {
            this.logger.EncoderDrainFailed("video", ex);
        }
    }


    void WriteSample(Java.Nio.ByteBuffer buffer, MediaCodec.BufferInfo info)
    {
        var raw = info.PresentationTimeUs;
        long mapped;

        // the compositor keeps drawing while paused; those frames are dropped here and the span
        // they covered is taken out of every later timestamp once the first frame after resume
        // arrives - see PauseTimeline
        lock (this.pauseGate)
        {
            if (!this.timeline.TryMap(raw, out mapped))
                return;
        }

        info.PresentationTimeUs = mapped;
        if (!this.muxer.Write(this.trackIndex, buffer, info))
            return;

        lock (this.pauseGate)
            this.timeline.Commit(raw, mapped);
    }


    /// <summary>Flushes the encoder and waits for the drain loop to finish.</summary>
    public void Stop()
    {
        if (!this.draining)
            return;

        try
        {
            // tells the encoder no more frames are coming, so it emits the end-of-stream flag the
            // drain loop breaks on rather than being cut off mid-frame
            this.codec.SignalEndOfInputStream();
        }
        catch (Java.Lang.IllegalStateException)
        {
            // the codec is already gone, so no end-of-stream is coming and the loop has nothing to
            // wait for - drop out of it now rather than spending the join timeout on nothing
            this.draining = false;
        }

        // generous, because the encoder may still be working through queued frames; the loop also
        // exits on the end-of-stream flag, so this only matters when the codec has wedged
        this.drainThread.Join(TimeSpan.FromSeconds(5));
        this.draining = false;

        try
        {
            this.codec.Stop();
        }
        catch (Java.Lang.IllegalStateException)
        {
            // stopping an already-stopped codec
        }
    }


    public void Dispose()
    {
        this.draining = false;
        this.Stop();
        this.InputSurface.Release();
        this.InputSurface.Dispose();
        this.codec.Release();
        this.codec.Dispose();
    }
}
