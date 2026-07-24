namespace Shiny.Calendar;

/// <summary>
/// A calendar available on the device (local, or synced from an account such as iCloud / Google / Exchange).
/// </summary>
public class Calendar
{
    public Calendar() { }

    public Calendar(string id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    /// <summary>The platform-assigned calendar identifier.</summary>
    public string? Id { get; set; }

    /// <summary>The display name of the calendar.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The calendar colour as a hex string (e.g. <c>#FF3B30</c>), or null if unknown. A string is used
    /// (rather than a MAUI <c>Color</c>) so the core library stays free of a MAUI dependency.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// True when events in this calendar cannot be modified (subscribed/holiday/birthday calendars, etc.).
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>The owning account / source of the calendar (e.g. iCloud, a Google account), when available.</summary>
    public string? Account { get; set; }

    public override string ToString() => this.Name;
}
