using Shiny.Infrastructure;
using Shiny.ScreenRecorder;

namespace Sample.Shared.Maui.Pages.ScreenRecorder;


[ShellMap<ScreenRecorderPage>("screenrecorder")]
public partial class ScreenRecorderViewModel(
    IScreenRecorder recorder,
    IMainThread mainThread
) : ObservableObject, IDisposable
{
    IScreenRecording? session;
    IDispatcherTimer? ticker;

    [ObservableProperty] string status = "Idle";
    [ObservableProperty] string capabilities = String.Empty;
    [ObservableProperty] string elapsed = "00:00";
    [ObservableProperty] string lastResult = "(nothing recorded yet)";
    [ObservableProperty] bool includeMicrophone;
    [ObservableProperty] bool includeSystemAudio;
    [ObservableProperty] bool isBusy;

    List<TargetItem> targets = [];
    public List<TargetItem> Targets
    {
        get => this.targets;
        private set
        {
            this.targets = value;
            this.OnPropertyChanged();
        }
    }

    TargetItem? selectedTarget;
    public TargetItem? SelectedTarget
    {
        get => this.selectedTarget;
        set
        {
            this.selectedTarget = value;
            this.OnPropertyChanged();
        }
    }

    // the page only offers what this platform actually supports - see ScreenRecorderCapabilities
    public bool CanRecord => recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.Recording);
    public bool CanPause => recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.PauseResume);
    public bool CanUseMicrophone => recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.Microphone);
    public bool CanUseSystemAudio => recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.SystemAudio);
    public bool CanPickTarget => recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.DisplaySelection);

    public bool IsRecording => this.session != null;
    public bool IsNotRecording => this.session == null;
    public bool IsPaused => this.session?.IsPaused ?? false;


    [RelayCommand]
    async Task Load()
    {
        this.Capabilities = recorder.Capabilities == ScreenRecorderCapabilities.None
            ? "None - no screen capture API on this platform"
            : recorder.Capabilities.ToString();

        if (!this.CanPickTarget)
            return;

        try
        {
            var found = await recorder.GetTargets();
            this.Targets = found
                .Select(x => new TargetItem(x, $"{x.Name}{(x.ApplicationName == null ? "" : $" - {x.ApplicationName}")}"))
                .ToList();
        }
        catch (ScreenRecorderPermissionException ex)
        {
            // macOS: the Screen Recording grant is missing, and listing shareable content is the
            // first thing that notices
            this.Status = ex.Message;
        }
    }


    [RelayCommand]
    async Task Start()
    {
        if (this.session != null)
            return;

        this.IsBusy = true;
        try
        {
            var request = new ScreenRecordingRequest
            {
                Target = this.SelectedTarget?.Target,
                IncludeMicrophone = this.IncludeMicrophone && this.CanUseMicrophone,
                IncludeSystemAudio = this.IncludeSystemAudio && this.CanUseSystemAudio,

                // a phone or Retina display at native resolution produces an enormous file for
                // very little visible gain
                MaxWidth = recorder.Capabilities.HasFlag(ScreenRecorderCapabilities.Downscaling) ? 1280 : null,
                MaxDuration = TimeSpan.FromMinutes(2)
            };

            var access = await recorder.RequestAccess(request);
            if (access is AccessState.Denied or AccessState.NotSupported)
            {
                this.Status = $"Access: {access}";
                return;
            }

            this.Status = "Starting - waiting for consent...";

            // does not return until frames are actually being written
            this.session = await recorder.Start(request);
            this.session.Faulted += this.OnFaulted;

            this.Status = "Recording";
            this.StartTicker();
            this.RaiseSessionState();
        }
        catch (ScreenRecorderPermissionException ex)
        {
            this.Status = $"Declined - {ex.Message}";
        }
        catch (ScreenRecorderNotSupportedException ex)
        {
            this.Status = $"Not supported - {ex.Message}";
        }
        catch (Exception ex)
        {
            this.Status = $"Failed - {ex.Message}";
        }
        finally
        {
            this.IsBusy = false;
        }
    }


    [RelayCommand]
    async Task TogglePause()
    {
        if (this.session == null)
            return;

        if (this.session.IsPaused)
        {
            await this.session.Resume();
            this.Status = "Recording";
        }
        else
        {
            await this.session.Pause();
            this.Status = "Paused";
        }

        this.RaiseSessionState();
    }


    [RelayCommand]
    async Task Stop()
    {
        if (this.session == null)
            return;

        this.IsBusy = true;
        try
        {
            var result = await this.session.Stop();
            this.Describe(result);
            this.Status = "Stopped";
        }
        catch (Exception ex)
        {
            this.Status = $"Stop failed - {ex.Message}";
        }
        finally
        {
            this.ClearSession();
            this.IsBusy = false;
        }
    }


    [RelayCommand]
    async Task Cancel()
    {
        if (this.session == null)
            return;

        // cancelling deletes the partial file - the same thing disposing without stopping does
        await this.session.Cancel();
        this.ClearSession();
        this.Status = "Cancelled";
        this.LastResult = "(discarded)";
    }


    // the OS ended it: the user hit the cast notification, a call came in, the encoder failed
    void OnFaulted(object? sender, ScreenRecordingFaultedEventArgs e) => mainThread.BeginInvokeOnMainThread(() =>
    {
        this.Status = $"Ended by the system - {e.Reason}";

        if (e.Result != null)
            this.Describe(e.Result);

        this.ClearSession();
    });


    void Describe(ScreenRecordingResult result)
        => this.LastResult =
            $"{result.Duration:mm\\:ss} - {result.Width}x{result.Height} {result.MimeType}\n" +
            $"{result.ByteSize / 1024d / 1024d:0.0} MB\n" +
            (result.FilePath ?? "(no file - browser recording)");


    void StartTicker()
    {
        this.ticker ??= Application.Current!.Dispatcher.CreateTimer();
        this.ticker.Interval = TimeSpan.FromMilliseconds(500);
        this.ticker.Tick -= this.OnTick;
        this.ticker.Tick += this.OnTick;
        this.ticker.Start();
    }


    void OnTick(object? sender, EventArgs e)
        => this.Elapsed = (this.session?.Elapsed ?? TimeSpan.Zero).ToString(@"mm\:ss");


    void ClearSession()
    {
        if (this.session != null)
            this.session.Faulted -= this.OnFaulted;

        this.session = null;
        this.ticker?.Stop();
        this.RaiseSessionState();
    }


    void RaiseSessionState()
    {
        this.OnPropertyChanged(nameof(this.IsRecording));
        this.OnPropertyChanged(nameof(this.IsNotRecording));
        this.OnPropertyChanged(nameof(this.IsPaused));
    }


    public void Dispose()
    {
        this.ticker?.Stop();

        // a session left running would keep the OS recording indicator up after the page is gone
        _ = this.session?.DisposeAsync().AsTask();
        this.session = null;
    }
}


public record TargetItem(CaptureTarget Target, string Display);
