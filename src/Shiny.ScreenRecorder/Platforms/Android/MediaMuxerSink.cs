using Android.Media;
using Java.Nio;
using Microsoft.Extensions.Logging;

namespace Shiny.ScreenRecorder;


/// <summary>
/// Wraps <see cref="MediaMuxer"/> so two encoders running on two threads can write into one MP4.
/// </summary>
/// <remarks>
/// <para>MediaMuxer has two rules that are easy to break and fail loudly: every track must be added
/// before <c>start()</c>, and nothing may be written before it. But each encoder only learns its
/// real output format part-way through, when MediaCodec emits
/// <c>INFO_OUTPUT_FORMAT_CHANGED</c> - which is also the first moment it has samples to write. So
/// this counts the tracks it is expecting, starts the muxer on the last registration, and makes
/// every writer block until that happens.</para>
/// <para>It is also the only place MediaMuxer is touched, from any thread, under one lock -
/// MediaMuxer is not thread-safe and the video and audio drain loops are genuinely concurrent.</para>
/// </remarks>
internal sealed class MediaMuxerSink : IDisposable
{
    readonly object gate = new();
    readonly MediaMuxer muxer;
    readonly ILogger logger;
    readonly int expectedTracks;

    int registeredTracks;
    bool started;
    bool stopped;


    public MediaMuxerSink(string outputPath, int expectedTracks, ILogger logger)
    {
        this.expectedTracks = expectedTracks;
        this.logger = logger;
        this.muxer = new MediaMuxer(outputPath, MuxerOutputType.Mpeg4);
    }


    /// <summary>Whether the muxer has started and samples may be written.</summary>
    public bool IsStarted
    {
        get
        {
            lock (this.gate)
                return this.started;
        }
    }


    /// <summary>
    /// Registers one encoder's output format, starting the muxer once every expected track is in.
    /// </summary>
    public int AddTrack(MediaFormat format)
    {
        lock (this.gate)
        {
            if (this.started)
                throw new ScreenRecorderException("A track was added after the muxer started - this is a bug in the recording pipeline");

            var index = this.muxer.AddTrack(format);
            this.registeredTracks++;

            if (this.registeredTracks == this.expectedTracks)
            {
                this.muxer.Start();
                this.started = true;
            }

            return index;
        }
    }


    /// <summary>
    /// Writes one encoded sample. Silently drops it when the muxer has not started yet or has
    /// already stopped.
    /// </summary>
    /// <remarks>
    /// Dropping is correct rather than lazy. Before the muxer starts, the other encoder has not
    /// produced a format yet and the samples in hand are the leading fraction of a second the
    /// recording is allowed to lose; after it stops, anything still in flight belongs to a file
    /// that is already finalised.
    /// </remarks>
    public void Write(int trackIndex, ByteBuffer buffer, MediaCodec.BufferInfo info)
    {
        lock (this.gate)
        {
            if (!this.started || this.stopped || trackIndex < 0 || info.Size <= 0)
                return;

            try
            {
                this.muxer.WriteSampleData(trackIndex, buffer, info);
            }
            catch (Exception ex)
            {
                this.logger.MuxerWriteFailed(ex);
            }
        }
    }


    /// <summary>Finalises the file. Returns false when nothing was ever written.</summary>
    public bool Stop()
    {
        lock (this.gate)
        {
            if (this.stopped)
                return false;

            this.stopped = true;

            if (!this.started)
                return false;

            try
            {
                this.muxer.Stop();
                return true;
            }
            catch (Exception ex)
            {
                // MediaMuxer.stop() throws when no sample reached it, which leaves an MP4 with no
                // moov atom - unplayable, and worth reporting as a failure rather than a file
                this.logger.MuxerStopFailed(ex);
                return false;
            }
        }
    }


    public void Dispose()
    {
        this.Stop();

        lock (this.gate)
            this.muxer.Release();

        this.muxer.Dispose();
    }
}
