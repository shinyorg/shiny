namespace Shiny.ScreenRecorder.Encoders;


/// <summary>Which external encoder a Linux recording will drive.</summary>
internal enum EncoderKind
{
    /// <summary>Nothing usable is installed.</summary>
    None,

    /// <summary>
    /// <c>gst-launch-1.0</c> reading the PipeWire node the portal handed over. Works on Wayland and
    /// X11 alike, and is the only option under Wayland.
    /// </summary>
    GStreamer,

    /// <summary>
    /// <c>ffmpeg</c> grabbing the X11 display directly. Only usable on X11, and only when there is
    /// no portal - it captures the whole display with no user consent step.
    /// </summary>
    FfmpegX11
}
