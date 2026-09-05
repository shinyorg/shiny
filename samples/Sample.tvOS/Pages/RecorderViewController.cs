using Sample.tvOS.Infrastructure;
using Shiny;
using Shiny.ScreenRecorder;

namespace Sample.tvOS.Pages;


/// <summary>
/// ReplayKit records this app's own UI, as it does on iOS - not the tvOS home screen or anyone
/// else's app. The one capability missing versus iOS is the microphone: an Apple TV has none, so
/// Microphone is not advertised and a request that sets IncludeMicrophone is rejected outright.
/// </summary>
public class RecorderViewController() : ModuleViewController(
    "Shiny.ScreenRecorder - ReplayKit, this app's UI only. No microphone: an Apple TV has none"
)
{
    IScreenRecording? recording;


    protected override void OnReady()
    {
        this.AddAction("Capabilities", () =>
        {
            var recorder = Resolve<IScreenRecorder>();
            this.ClearLog();
            this.Log($"capabilities: {recorder.Capabilities}");
            this.Log($"microphone:   {recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.Microphone)}  (false on tvOS)");
            this.Log($"system audio: {recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.SystemAudio)}");
            this.Log($"pause/resume: {recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.PauseResume)}");
            return Task.CompletedTask;
        });

        this.AddAction("Start", async () =>
        {
            if (this.recording != null)
            {
                this.Log("already recording");
                return;
            }

            var recorder = Resolve<IScreenRecorder>();
            var platform = Resolve<IPlatform>();

            var request = new ScreenRecordingRequest
            {
                OutputPath = Path.Combine(platform.Cache.FullName, $"recording-{DateTime.Now:HHmmss}.mp4"),
                IncludeSystemAudio = true,
                // IncludeMicrophone = true would throw here - Microphone is not in Capabilities on tvOS
                MaxDuration = TimeSpan.FromMinutes(1)
            };

            var access = await recorder.RequestAccess(request);
            this.Log($"access: {access}");
            if (access != AccessState.Available)
                return;

            this.recording = await recorder.Start(request);
            this.recording.Faulted += (_, args) => this.Log($"the OS ended the recording: {args.Reason}");
            this.Log("recording this app's UI...");
        });

        this.AddAction("Pause", async () =>
        {
            if (this.recording == null)
            {
                this.Log("not recording");
                return;
            }
            await this.recording.Pause();
            this.Log("paused");
        });

        this.AddAction("Stop", async () =>
        {
            if (this.recording == null)
            {
                this.Log("not recording");
                return;
            }

            var result = await this.recording.Stop();
            await this.recording.DisposeAsync();
            this.recording = null;

            this.Log($"stopped: {result.Duration:mm\\:ss}  {result.Width}x{result.Height}  {result.ByteSize / 1024}KB");
            this.Log($"file: {result.FilePath ?? "(no path)"}");
        });
    }
}
