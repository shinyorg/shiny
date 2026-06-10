namespace Sample.Shared.Maui.Services;

/// <summary>
/// Tracks whether the app UI is currently in the foreground. Defaults to false so events
/// recorded before a window exists (headless/background launches) report as background.
/// </summary>
public sealed class AppStateTracker
{
    public bool IsInForeground { get; private set; }

    public void Track(Window window)
    {
        window.Activated += (_, _) => this.IsInForeground = true;
        window.Resumed += (_, _) => this.IsInForeground = true;
        window.Stopped += (_, _) => this.IsInForeground = false;
        window.Destroying += (_, _) => this.IsInForeground = false;
    }
}
