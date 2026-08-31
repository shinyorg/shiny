namespace Shiny.ScreenRecorder;


/// <summary>Which writer input a sample buffer belongs to.</summary>
internal enum AssetWriterTrack
{
    Video,

    /// <summary>Audio the device is playing - ReplayKit's app audio, ScreenCaptureKit's system audio.</summary>
    SystemAudio,

    /// <summary>The microphone.</summary>
    Microphone
}
