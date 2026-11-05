using Microsoft.Extensions.Logging;

namespace Sample.Uno;


/// <summary>
/// Lets code Shiny can run before any view exists - the notification delegate, startup tasks - hand state to the UI.
/// </summary>
public class SampleState
{
    public event EventHandler? Changed;

    /// <summary>When the Shiny host started - on Android, before any activity, from Application.OnCreate.</summary>
    public DateTimeOffset? ShinyStarted { get; set; }

    public string? LastNotificationTap
    {
        get;
        set
        {
            field = value;
            this.Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}


/// <summary>
/// Records when Shiny's startup tasks ran, so the page can show the host came up before the UI did.
/// </summary>
public class StartupRecorder(SampleState state, ILogger<StartupRecorder> logger) : Shiny.IShinyStartupTask
{
    public void Start()
    {
        state.ShinyStarted = DateTimeOffset.Now;
        logger.LogInformation("Shiny host started at {Time:HH:mm:ss.fff}", state.ShinyStarted);
    }
}
