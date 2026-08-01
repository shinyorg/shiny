namespace Shiny.Calendar.Extensions.AI.Internal;

/// <summary>
/// Resolves what the LLM may do with a given calendar: a global capability set that applies to every
/// calendar on the device, plus optional per-calendar-id entries that replace the global set outright
/// (so an entry can widen <i>or</i> narrow access, including <see cref="CalendarAICapabilities.None"/>
/// to hide a calendar completely).
/// </summary>
sealed class CalendarAccessPolicy
{
    readonly IReadOnlyDictionary<string, CalendarAICapabilities> perCalendar;

    public CalendarAccessPolicy(
        CalendarAICapabilities global,
        IReadOnlyDictionary<string, CalendarAICapabilities> perCalendar
    )
    {
        this.Global = global;
        this.perCalendar = perCalendar;

        var effective = global;
        foreach (var caps in perCalendar.Values)
            effective |= caps;
        this.Effective = effective;
    }

    /// <summary>Capabilities applied to any calendar without an explicit entry.</summary>
    public CalendarAICapabilities Global { get; }

    /// <summary>
    /// The union of every capability granted anywhere. This decides which tools are handed to the LLM
    /// at all - the tools themselves still enforce the per-calendar rules on each call.
    /// </summary>
    public CalendarAICapabilities Effective { get; }

    /// <summary>True when at least one calendar id was given an explicit capability set.</summary>
    public bool HasCalendarFilters => this.perCalendar.Count > 0;

    /// <summary>The capabilities allowed for a calendar id (null id falls back to the global set).</summary>
    public CalendarAICapabilities For(string? calendarId)
        => calendarId != null && this.perCalendar.TryGetValue(calendarId, out var caps)
            ? caps
            : this.Global;

    /// <summary>True when <paramref name="capability"/> is allowed on the given calendar.</summary>
    public bool Allows(string? calendarId, CalendarAICapabilities capability)
        => this.For(calendarId).HasFlag(capability);

    /// <summary>
    /// True when <paramref name="capability"/> is granted for every calendar on the device - i.e. it is
    /// in the global set and no per-calendar entry takes it away. Lets the tools skip the extra
    /// calendar round-trips needed to enforce an allow-list.
    /// </summary>
    public bool AppliesToAll(CalendarAICapabilities capability)
    {
        if (!this.Global.HasFlag(capability))
            return false;

        foreach (var caps in this.perCalendar.Values)
        {
            if (!caps.HasFlag(capability))
                return false;
        }
        return true;
    }

    /// <summary>The calendar ids explicitly granted <paramref name="capability"/> by an id entry.</summary>
    public IReadOnlyList<string> ExplicitlyAllowed(CalendarAICapabilities capability)
        => this.perCalendar
            .Where(x => x.Value.HasFlag(capability))
            .Select(x => x.Key)
            .ToList();
}
