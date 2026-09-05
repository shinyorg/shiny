namespace Sample.tvOS.Services;


/// <summary>
/// A tiny in-memory log the background-facing pieces (job, transfer delegate, push delegate,
/// sync delegate) write to so the UI can show what happened while it was not on screen.
/// </summary>
public class AppLog
{
    readonly List<string> entries = new();
    readonly Lock sync = new();

    public event EventHandler<string>? Written;

    public void Write(string message)
    {
        lock (this.sync)
        {
            this.entries.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
            if (this.entries.Count > 200)
                this.entries.RemoveRange(200, this.entries.Count - 200);
        }
        this.Written?.Invoke(this, message);
    }

    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (this.sync)
                return this.entries.ToList();
        }
    }
}
