using AVFoundation;
using CoreMedia;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.ScreenRecorder.Infrastructure;

namespace Shiny.ScreenRecorder;


/// <summary>
/// Writes CMSampleBuffers to an MP4 through <see cref="AVAssetWriter"/>, with a synthesised pause.
/// </summary>
/// <remarks>
/// <para>Shared by ReplayKit (iOS, Mac Catalyst) and by the ScreenCaptureKit path on macOS 12.3-14.
/// Neither of those APIs writes a file for you and neither can pause, and both hand over buffers
/// the same way - so the writer, the timestamp arithmetic and the pause are all solved once here.
/// macOS 15+ does not use this at all: <c>SCRecordingOutput</c> writes the file itself.</para>
/// <para><b>Pause is a timestamp rewrite, not a stop.</b> The capture keeps producing buffers while
/// paused; they are dropped, and the span they covered is accumulated into <see cref="offset"/> and
/// subtracted from every later buffer. Without that subtraction the file would contain a gap and
/// most players would sit frozen on the last frame for the length of the pause.</para>
/// <para>Everything is serialised on one lock. The buffer callbacks arrive on a dispatch queue,
/// pause/resume/finish arrive on the caller's thread, and AVAssetWriter is not thread-safe across
/// those.</para>
/// </remarks>
internal sealed class AssetWriterSink : IDisposable
{
    // the media type and file type constants are NSStrings behind enums in the binding; resolving
    // them once avoids a native round trip per writer
    static readonly string VideoMediaType = AVMediaTypes.Video.GetConstant()!.ToString();
    static readonly string AudioMediaType = AVMediaTypes.Audio.GetConstant()!.ToString();
    static readonly string Mp4FileType = AVFileTypes.Mpeg4.GetConstant()!.ToString();

    readonly object gate = new();
    readonly ILogger logger;
    readonly AVAssetWriter writer;
    readonly AVAssetWriterInput videoInput;
    readonly AVAssetWriterInput? systemAudioInput;
    readonly AVAssetWriterInput? micInput;

    bool sessionStarted;
    bool finished;
    bool paused;
    CMTime offset = CMTime.Zero;
    CMTime pauseStartedAt = CMTime.Invalid;
    CMTime lastVideoTime = CMTime.Invalid;


    public AssetWriterSink(
        string outputPath,
        VideoDimensions dimensions,
        bool includeSystemAudio,
        bool includeMicrophone,
        ILogger logger
    )
    {
        this.logger = logger;
        this.OutputPath = outputPath;
        this.Dimensions = dimensions;

        // AVAssetWriter refuses to initialise onto an existing file rather than overwriting it
        if (File.Exists(outputPath))
            File.Delete(outputPath);

        this.writer = AVAssetWriter.FromUrl(NSUrl.FromFilename(outputPath), Mp4FileType, out var error)
            ?? throw new ScreenRecorderException($"Could not create the video writer - {error?.LocalizedDescription ?? "unknown error"}");

        this.videoInput = new AVAssetWriterInput(VideoMediaType, new AVVideoSettingsCompressed
        {
            CodecType = AVVideoCodecType.H264,
            Width = dimensions.Width,
            Height = dimensions.Height,

            // screen content is long stretches of identical frames punctuated by sharp changes;
            // a keyframe every 2s keeps seeking usable without spending the bitrate on I-frames
            MaxKeyFrameIntervalDuration = 2f,
            CodecSettings = new AVVideoCodecSettings
            {
                AverageBitRate = dimensions.Bitrate
            }
        })
        {
            // the buffers are arriving live off the screen, so the writer must not assume it can
            // block the producer while it catches up
            ExpectsMediaDataInRealTime = true
        };
        this.writer.AddInput(this.videoInput);

        if (includeSystemAudio)
            this.systemAudioInput = this.AddAudioInput();

        if (includeMicrophone)
            this.micInput = this.AddAudioInput();
    }


    public string OutputPath { get; }
    public VideoDimensions Dimensions { get; }

    /// <summary>The duration actually written, which excludes any paused span.</summary>
    public TimeSpan Duration => this.lastVideoTime.IsNumeric
        ? TimeSpan.FromSeconds(Math.Max(0, this.lastVideoTime.Seconds))
        : TimeSpan.Zero;


    AVAssetWriterInput AddAudioInput()
    {
        // no explicit settings: passing null lets AVFoundation choose an AAC configuration that
        // matches the incoming format description, which is what both capture APIs hand us and is
        // far more robust than guessing a sample rate and channel count that must match exactly
        var input = new AVAssetWriterInput(AudioMediaType, (AudioSettings?)null)
        {
            ExpectsMediaDataInRealTime = true
        };
        this.writer.AddInput(input);

        return input;
    }


    /// <summary>
    /// Appends a buffer to its track, starting the writing session off the first video buffer.
    /// </summary>
    /// <remarks>
    /// Audio that arrives before the first video frame is dropped rather than queued. Starting the
    /// session on an audio timestamp and then receiving an earlier video one makes AVAssetWriter
    /// fail the whole file, and a few milliseconds of leading audio is not worth that risk.
    /// </remarks>
    public void Append(CMSampleBuffer buffer, AssetWriterTrack track)
    {
        lock (this.gate)
        {
            if (this.finished || !buffer.DataIsReady)
                return;

            var pts = buffer.PresentationTimeStamp;
            if (!pts.IsNumeric)
                return;

            if (!this.sessionStarted)
            {
                if (track != AssetWriterTrack.Video)
                    return;

                if (!this.writer.StartWriting())
                    throw new ScreenRecorderException($"The video writer refused to start - {this.writer.Error?.LocalizedDescription ?? "unknown error"}");

                this.writer.StartSessionAtSourceTime(pts);
                this.sessionStarted = true;
            }

            if (this.paused)
            {
                // remember where the pause began so Resume knows how much to discount
                if (!this.pauseStartedAt.IsNumeric)
                    this.pauseStartedAt = pts;

                return;
            }

            var input = track switch
            {
                AssetWriterTrack.Video => this.videoInput,
                AssetWriterTrack.SystemAudio => this.systemAudioInput,
                AssetWriterTrack.Microphone => this.micInput,
                _ => null
            };

            if (input == null || !input.ReadyForMoreMediaData)
                return;

            var adjusted = this.Retime(buffer, pts);
            try
            {
                if (!input.AppendSampleBuffer(adjusted))
                {
                    this.logger.AppendRejected(track.ToString(), this.writer.Error?.LocalizedDescription ?? "unknown");
                    return;
                }

                if (track == AssetWriterTrack.Video)
                    this.lastVideoTime = CMTime.Subtract(pts, this.offset);
            }
            finally
            {
                if (!ReferenceEquals(adjusted, buffer))
                    adjusted.Dispose();
            }
        }
    }


    // shifting every buffer back by the accumulated paused span is what removes the gap; when
    // nothing has been paused the offset is zero and the original buffer is passed straight
    // through rather than copied for nothing
    CMSampleBuffer Retime(CMSampleBuffer buffer, CMTime pts)
    {
        if (this.offset == CMTime.Zero)
            return buffer;

        var timing = buffer.GetSampleTimingInfo();
        if (timing == null || timing.Length == 0)
        {
            this.logger.RetimeFailed(pts.Seconds);
            return buffer;
        }

        for (var i = 0; i < timing.Length; i++)
        {
            timing[i].PresentationTimeStamp = CMTime.Subtract(timing[i].PresentationTimeStamp, this.offset);

            if (timing[i].DecodeTimeStamp.IsNumeric)
                timing[i].DecodeTimeStamp = CMTime.Subtract(timing[i].DecodeTimeStamp, this.offset);
        }

        var copy = CMSampleBuffer.CreateWithNewTiming(buffer, timing);
        if (copy == null)
        {
            this.logger.RetimeFailed(pts.Seconds);
            return buffer;
        }

        return copy;
    }


    public void Pause()
    {
        lock (this.gate)
        {
            this.paused = true;
            this.pauseStartedAt = CMTime.Invalid;
        }
    }


    public void Resume()
    {
        lock (this.gate)
        {
            if (!this.paused)
                return;

            this.paused = false;

            // no buffer arrived while paused, so nothing was actually skipped
            if (!this.pauseStartedAt.IsNumeric || !this.lastVideoTime.IsNumeric)
            {
                this.pauseStartedAt = CMTime.Invalid;
                return;
            }

            // the span to discount runs from the first dropped buffer to now; "now" is derived from
            // the last buffer actually written plus the offset already in effect, which is the same
            // clock the incoming timestamps are on
            var resumeAt = this.pauseStartedAt;
            var lastWritten = CMTime.Add(this.lastVideoTime, this.offset);
            var skipped = CMTime.Subtract(resumeAt, lastWritten);

            if (skipped.IsNumeric && CMTime.Compare(skipped, CMTime.Zero) > 0)
                this.offset = CMTime.Add(this.offset, skipped);

            this.pauseStartedAt = CMTime.Invalid;
        }
    }


    /// <summary>Flushes the encoder and finalises the container.</summary>
    public async Task<bool> Finish()
    {
        lock (this.gate)
        {
            if (this.finished)
                return false;

            this.finished = true;

            if (!this.sessionStarted)
                return false;

            this.videoInput.MarkAsFinished();
            this.systemAudioInput?.MarkAsFinished();
            this.micInput?.MarkAsFinished();

            if (this.lastVideoTime.IsNumeric)
                this.writer.EndSessionAtSourceTime(this.lastVideoTime);
        }

        await this.writer.FinishWritingAsync().ConfigureAwait(false);

        if (this.writer.Status == AVAssetWriterStatus.Failed)
            throw new ScreenRecorderException($"Finalising the recording failed - {this.writer.Error?.LocalizedDescription ?? "unknown error"}");

        return true;
    }


    /// <summary>Abandons the recording without producing a playable file.</summary>
    public void Abort()
    {
        lock (this.gate)
        {
            if (this.finished)
                return;

            this.finished = true;

            if (this.sessionStarted)
                this.writer.CancelWriting();
        }
    }


    public void Dispose()
    {
        this.Abort();
        this.videoInput.Dispose();
        this.systemAudioInput?.Dispose();
        this.micInput?.Dispose();
        this.writer.Dispose();
    }
}
