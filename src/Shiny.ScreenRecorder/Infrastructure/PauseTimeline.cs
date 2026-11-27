namespace Shiny.ScreenRecorder.Infrastructure;


/// <summary>
/// Maps a capture's raw timestamps onto the output timeline with every paused span removed.
/// </summary>
/// <remarks>
/// <para>Shared by every backend that synthesises pause (Android, ReplayKit, ScreenCaptureKit
/// below macOS 15, Windows). Each of them keeps receiving frames while paused, drops them, and has
/// to shift every later frame back by however long the pause lasted.</para>
/// <para><b>The span removed runs from where the pause began to the first frame after resume.</b>
/// "Where the pause began" is the first frame dropped, or the last frame written when nothing
/// arrived while paused (a screen that did not change). The first frame after resume is the only
/// point at which the length of the pause is actually known - which is why the offset is applied
/// there, in <see cref="TryMap"/>, and not in <see cref="Resume"/>.</para>
/// <para>Units are whatever the caller uses (microseconds, ticks, nanoseconds) as long as they are
/// consistent. Not thread-safe - callers already serialise around their own lock.</para>
/// </remarks>
internal sealed class PauseTimeline
{
    long offset;
    long? firstMapped;
    long? lastMapped;
    long? lastWrittenRaw;
    long? firstDroppedRaw;
    bool paused;
    bool resumePending;


    /// <summary>Whether frames are currently being dropped.</summary>
    public bool IsPaused => this.paused;

    /// <summary>The span written so far, from the first mapped timestamp to the last.</summary>
    public long Duration => this.firstMapped is { } first && this.lastMapped is { } last ? last - first : 0;

    /// <summary>The last timestamp handed out, on the output timeline.</summary>
    public long? LastMapped => this.lastMapped;

    /// <summary>The first timestamp handed out, on the output timeline.</summary>
    public long? FirstMapped => this.firstMapped;


    public void Pause()
    {
        if (this.paused)
            return;

        this.paused = true;

        // a resume that no frame followed has not closed its gap yet; the anchor it was going to
        // use stays, so the second pause extends the first rather than replacing it
        if (!this.resumePending)
            this.firstDroppedRaw = null;

        this.resumePending = false;
    }


    public void Resume()
    {
        if (!this.paused)
            return;

        this.paused = false;
        this.resumePending = true;
    }


    /// <summary>
    /// Maps one raw timestamp. False means the frame falls inside a pause and must be dropped.
    /// </summary>
    /// <remarks>
    /// Only call <see cref="Commit"/> with the result once the frame really was written, so a frame
    /// the encoder or muxer refused does not move the timeline.
    /// </remarks>
    public bool TryMap(long raw, out long mapped)
    {
        if (this.paused)
        {
            this.firstDroppedRaw ??= raw;
            mapped = 0;
            return false;
        }

        if (!this.resumePending)
        {
            mapped = raw - this.offset;
            return true;
        }

        this.resumePending = false;

        var anchor = this.firstDroppedRaw ?? this.lastWrittenRaw;
        if (anchor is { } start && raw > start)
            this.offset += raw - start;

        this.firstDroppedRaw = null;
        mapped = raw - this.offset;

        // with no frame dropped the anchor is the last frame written, which would hand the first
        // frame after resume the same timestamp - encoders and muxers reject a timestamp that does
        // not move forward, so it is nudged one unit past. Only here: in ordinary running, tracks
        // sharing a timeline interleave and an audio sample may legitimately sit behind the last
        // video frame
        if (this.lastMapped is { } last && mapped <= last)
        {
            this.offset = raw - (last + 1);
            mapped = last + 1;
        }

        return true;
    }


    /// <summary>Records a frame as written, at the raw and mapped timestamps it was given.</summary>
    public void Commit(long raw, long mapped)
    {
        this.firstMapped ??= mapped;
        this.lastMapped = mapped;
        this.lastWrittenRaw = raw;
    }
}
