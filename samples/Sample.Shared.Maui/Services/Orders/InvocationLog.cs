namespace Sample.Shared.Maui.Services.Orders;

public record InvocationEntry(DateTime Timestamp, string Text);

/// <summary>Every assistant call, newest first, persisted so calls made while the app was closed are visible.</summary>
public class InvocationLog
{
    readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sample-appfunction-calls.log");
    readonly Lock sync = new();

    public event Action? Added;

    public IReadOnlyList<InvocationEntry> Entries
    {
        get
        {
            lock (this.sync)
            {
                if (!File.Exists(this.path))
                    return [];

                return File
                    .ReadAllLines(this.path)
                    .Reverse()
                    .Take(50)
                    .Select(Parse)
                    .ToList();
            }
        }
    }

    public void Add(string text)
    {
        lock (this.sync)
            File.AppendAllText(this.path, $"{DateTime.Now:O}\t{text.ReplaceLineEndings(" ")}{Environment.NewLine}");

        this.Added?.Invoke();
    }

    public void Clear()
    {
        lock (this.sync)
            File.Delete(this.path);

        this.Added?.Invoke();
    }

    static InvocationEntry Parse(string line)
    {
        var tab = line.IndexOf('\t');
        return tab > 0 && DateTime.TryParse(line[..tab], null, System.Globalization.DateTimeStyles.RoundtripKind, out var ts)
            ? new InvocationEntry(ts, line[(tab + 1)..])
            : new InvocationEntry(DateTime.MinValue, line);
    }
}
