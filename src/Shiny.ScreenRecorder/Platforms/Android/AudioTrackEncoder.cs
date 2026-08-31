using Android.Media;
using Android.Media.Projection;
using Microsoft.Extensions.Logging;

namespace Shiny.ScreenRecorder;


/// <summary>
/// Captures the microphone and/or the device's own playback, mixes them, and encodes to AAC.
/// </summary>
/// <remarks>
/// <para>This is the reason the Android backend uses MediaCodec rather than the far simpler
/// MediaRecorder. <c>MediaRecorder.setAudioSource</c> takes exactly one source, and playback
/// capture is not one of them - it only exists through
/// <c>AudioRecord</c> + <c>AudioPlaybackCaptureConfiguration</c>. Wanting app audio at all forces
/// the whole pipeline down to MediaCodec.</para>
/// <para><b>What playback capture actually captures is up to the apps being recorded.</b> An app
/// whose audio attributes are not <c>USAGE_MEDIA</c> or <c>USAGE_GAME</c>, or that has set
/// <c>allowAudioPlaybackCapture=false</c>, is silently absent from the mix - by design on
/// Android's part, and not something this can work around.</para>
/// <para>When both sources are on they are read in lockstep and summed. Two AudioRecords run off
/// separate clocks, so reading the same frame count from each and blocking on whichever is slower
/// is what keeps them aligned; over a long recording the mix follows the slower of the two rather
/// than drifting apart.</para>
/// </remarks>
internal sealed class AudioTrackEncoder : IDisposable
{
    const string MimeType = "audio/mp4a-latm";
    const int SampleRate = 44_100;
    const int ChannelCount = 2;
    const int BytesPerFrame = ChannelCount * 2;   // 16-bit stereo
    const int BitRate = 128_000;

    readonly MediaMuxerSink muxer;
    readonly ILogger logger;
    readonly MediaCodec codec;
    readonly AudioRecord? mic;
    readonly AudioRecord? playback;
    readonly Thread pumpThread;
    readonly int bufferSize;

    byte[]? transfer;
    int trackIndex = -1;
    volatile bool running = true;
    volatile bool paused;
    long framesWritten;


    public AudioTrackEncoder(
        MediaProjection projection,
        bool includeMicrophone,
        bool includeSystemAudio,
        MediaMuxerSink muxer,
        ILogger logger
    )
    {
        this.muxer = muxer;
        this.logger = logger;

        var minimum = AudioRecord.GetMinBufferSize(SampleRate, ChannelIn.Stereo, Encoding.Pcm16bit);
        if (minimum <= 0)
            throw new ScreenRecorderException("This device reported no usable audio buffer size for 44.1kHz stereo capture");

        // reading in chunks well above the minimum keeps the pump loop off the edge of an underrun
        // without adding latency anyone can hear in a recording
        this.bufferSize = Math.Max(minimum * 2, BytesPerFrame * 1024);

        if (includeMicrophone)
            this.mic = this.BuildMicrophoneRecord();

        if (includeSystemAudio)
            this.playback = this.BuildPlaybackRecord(projection);

        var format = MediaFormat.CreateAudioFormat(MimeType, SampleRate, ChannelCount)!;
        format.SetInteger(MediaFormat.KeyAacProfile, (int)MediaCodecProfileType.Aacobjectlc);
        format.SetInteger(MediaFormat.KeyBitRate, BitRate);
        format.SetInteger(MediaFormat.KeyMaxInputSize, this.bufferSize);

        this.codec = MediaCodec.CreateEncoderByType(MimeType)!;
        this.codec.Configure(format, null, null, MediaCodecConfigFlags.Encode);
        this.codec.Start();

        this.mic?.StartRecording();
        this.playback?.StartRecording();

        this.pumpThread = new Thread(this.Pump) { IsBackground = true, Name = "shiny-screen-audio" };
        this.pumpThread.Start();
    }


    AudioRecord BuildMicrophoneRecord()
        => new AudioRecord.Builder()
            .SetAudioSource(AudioSource.Mic)!
            .SetAudioFormat(new AudioFormat.Builder()
                .SetEncoding(Encoding.Pcm16bit)!
                .SetSampleRate(SampleRate)!
                .SetChannelMask(ChannelOut.Stereo)!
                .Build()!)!
            .SetBufferSizeInBytes(this.bufferSize)!
            .Build()
            ?? throw new ScreenRecorderException("Could not open the microphone. Check that RECORD_AUDIO is granted");


    AudioRecord BuildPlaybackRecord(MediaProjection projection)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            throw ScreenRecorderNotSupportedException.For(
                ScreenRecorderCapabilities.SystemAudio,
                "playback capture needs Android 10 (API 29) or later"
            );

        // media and game are the two usages an app is allowed to capture; notifications, alarms and
        // voice calls are excluded by the OS no matter what is asked for here
        var config = new AudioPlaybackCaptureConfiguration.Builder(projection)
            .AddMatchingUsage(AudioUsageKind.Media)!
            .AddMatchingUsage(AudioUsageKind.Game)!
            .Build()!;

        return new AudioRecord.Builder()
            .SetAudioPlaybackCaptureConfig(config)!
            .SetAudioFormat(new AudioFormat.Builder()
                .SetEncoding(Encoding.Pcm16bit)!
                .SetSampleRate(SampleRate)!
                .SetChannelMask(ChannelOut.Stereo)!
                .Build()!)!
            .SetBufferSizeInBytes(this.bufferSize)!
            .Build()
            ?? throw new ScreenRecorderException("Could not open playback capture");
    }


    public void Pause() => this.paused = true;
    public void Resume() => this.paused = false;


    void Pump()
    {
        var primary = new short[this.bufferSize / 2];
        var secondary = this.mic != null && this.playback != null ? new short[this.bufferSize / 2] : null;
        var info = new MediaCodec.BufferInfo();

        try
        {
            while (this.running)
            {
                var source = this.playback ?? this.mic!;
                var read = source.Read(primary, 0, primary.Length);

                if (read <= 0)
                {
                    this.DrainEncoder(info, endOfStream: false);
                    continue;
                }

                if (secondary != null)
                {
                    var other = this.mic!.Read(secondary, 0, read);
                    if (other > 0)
                        Mix(primary, secondary, Math.Min(read, other));
                }

                // paused audio is discarded rather than encoded; the presentation timestamps are
                // derived from the frame count, so skipping frames closes the gap automatically and
                // needs no offset arithmetic of its own
                if (!this.paused)
                    this.Feed(primary, read);

                this.DrainEncoder(info, endOfStream: false);
            }
        }
        catch (Java.Lang.IllegalStateException)
        {
            // the codec or the record was released by Stop()
        }
        catch (Exception ex)
        {
            this.logger.EncoderDrainFailed("audio", ex);
        }
    }


    // summed with saturation - wrapping on overflow turns a loud moment into a burst of noise,
    // which is far more noticeable than the clipping
    static void Mix(short[] destination, short[] source, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var sum = destination[i] + source[i];
            destination[i] = (short)Math.Clamp(sum, Int16.MinValue, Int16.MaxValue);
        }
    }


    void Feed(short[] samples, int count)
    {
        var index = this.codec.DequeueInputBuffer(10_000);
        if (index < 0)
            return;

        using var buffer = this.codec.GetInputBuffer(index);
        if (buffer == null)
            return;

        buffer.Clear();

        // the pump loop runs dozens of times a second for the life of the recording, so the
        // transfer buffer is allocated once rather than per chunk
        var byteCount = count * 2;
        this.transfer ??= new byte[this.bufferSize];
        Buffer.BlockCopy(samples, 0, this.transfer, 0, byteCount);
        buffer.Put(this.transfer, 0, byteCount);

        // timestamps come from how many frames have been handed over, not from the wall clock -
        // the wall clock includes the time spent in the read and would drift audibly out of sync
        var presentationUs = this.framesWritten * 1_000_000L / SampleRate;
        this.framesWritten += count / ChannelCount;

        this.codec.QueueInputBuffer(index, 0, byteCount, presentationUs, MediaCodecBufferFlags.None);
    }


    void DrainEncoder(MediaCodec.BufferInfo info, bool endOfStream)
    {
        while (true)
        {
            var index = this.codec.DequeueOutputBuffer(info, endOfStream ? 10_000 : 0);

            if (index == (int)MediaCodecInfoState.TryAgainLater)
                return;

            if (index == (int)MediaCodecInfoState.OutputFormatChanged)
            {
                this.trackIndex = this.muxer.AddTrack(this.codec.OutputFormat!);
                continue;
            }

            if (index < 0)
                return;

            using var buffer = this.codec.GetOutputBuffer(index);
            var isConfig = (info.Flags & MediaCodecBufferFlags.CodecConfig) != 0;

            if (!isConfig && buffer != null && info.Size > 0)
                this.muxer.Write(this.trackIndex, buffer, info);

            this.codec.ReleaseOutputBuffer(index, false);

            if ((info.Flags & MediaCodecBufferFlags.EndOfStream) != 0)
                return;
        }
    }


    public void Stop()
    {
        if (!this.running)
            return;

        this.running = false;
        this.pumpThread.Join(TimeSpan.FromSeconds(3));

        try
        {
            this.mic?.Stop();
            this.playback?.Stop();

            // an empty input buffer flagged end-of-stream is how a byte-fed codec is told to flush;
            // there is no SignalEndOfInputStream outside surface input
            var index = this.codec.DequeueInputBuffer(10_000);
            if (index >= 0)
            {
                var presentationUs = this.framesWritten * 1_000_000L / SampleRate;
                this.codec.QueueInputBuffer(index, 0, 0, presentationUs, MediaCodecBufferFlags.EndOfStream);
                this.DrainEncoder(new MediaCodec.BufferInfo(), endOfStream: true);
            }

            this.codec.Stop();
        }
        catch (Java.Lang.IllegalStateException)
        {
            // already torn down
        }
        catch (Exception ex)
        {
            this.logger.EncoderDrainFailed("audio", ex);
        }
    }


    public void Dispose()
    {
        this.Stop();
        this.mic?.Release();
        this.mic?.Dispose();
        this.playback?.Release();
        this.playback?.Dispose();
        this.codec.Release();
        this.codec.Dispose();
    }
}
