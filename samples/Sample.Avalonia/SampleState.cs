namespace Sample.Avalonia;


/// <summary>
/// Lets the notification delegate - which Shiny can run before any view exists - hand the last tap to the UI.
/// </summary>
public class SampleState
{
    public event EventHandler? Changed;

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
