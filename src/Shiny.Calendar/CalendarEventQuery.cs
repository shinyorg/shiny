using Shiny.Calendar.Internals;

namespace Shiny.Calendar;

/// <summary>Fields a <see cref="CalendarEventQuery"/> result can be sorted by.</summary>
public enum CalendarEventSortField
{
    Start,
    End,
    Title
}

/// <summary>
/// Fluent, async query builder returned by <see cref="ICalendarStore.Query"/>. Nothing runs until you
/// call <see cref="ToListAsync"/>, <see cref="FirstOrDefaultAsync"/> or <see cref="CountAsync"/>, and
/// the native read happens off the calling thread - it is safe to await from the UI thread.
/// <para>
/// The calendar id and the date window are pushed down to the native fetch; everything else
/// (<see cref="Where(Func{CalendarEvent, bool})"/>, sorting, paging) is applied to the fetched events.
/// When no window is set the platform's default range is used (implementation-defined, typically a few
/// months around today) - always set one for a predictable, cheap read.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var upcoming = await store
///     .Query()
///     .ForCalendar(calendarId)
///     .Between(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(30))
///     .OrderBy(CalendarEventSortField.Start)
///     .ToListAsync(ct);
/// </code>
/// </example>
public sealed class CalendarEventQuery
{
    readonly Func<CalendarEventQueryDescriptor, CancellationToken, Task<IEnumerable<CalendarEvent>>> executor;
    readonly CalendarEventQueryDescriptor descriptor = new();
    readonly List<Func<CalendarEvent, bool>> predicates = [];
    readonly List<(CalendarEventSortField Field, bool Descending)> sorts = [];

    /// <summary>
    /// Platform stores construct this with an executor that performs the native fetch for a
    /// <see cref="CalendarEventQueryDescriptor"/>. App code obtains one from <see cref="ICalendarStore.Query"/>.
    /// </summary>
    public CalendarEventQuery(Func<CalendarEventQueryDescriptor, CancellationToken, Task<IEnumerable<CalendarEvent>>> executor)
        => this.executor = executor;

    /// <summary>
    /// Restricts the query to a single calendar. Null (the default) queries all calendars.
    /// </summary>
    public CalendarEventQuery ForCalendar(string? calendarId)
    {
        this.descriptor.CalendarId = calendarId;
        return this;
    }

    /// <summary>Only events that end on or after this instant.</summary>
    public CalendarEventQuery From(DateTimeOffset start)
    {
        this.descriptor.StartAfter = start;
        return this;
    }

    /// <summary>Only events that start on or before this instant.</summary>
    public CalendarEventQuery To(DateTimeOffset end)
    {
        this.descriptor.EndBefore = end;
        return this;
    }

    /// <summary>Sets both ends of the date window. Equivalent to <see cref="From"/> + <see cref="To"/>.</summary>
    public CalendarEventQuery Between(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
            throw new ArgumentOutOfRangeException(nameof(end), "The end of the window must not precede its start.");

        return this.From(start).To(end);
    }

    /// <summary>
    /// Adds an in-memory filter applied to the fetched events. Multiple predicates are ANDed.
    /// </summary>
    public CalendarEventQuery Where(Func<CalendarEvent, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        this.predicates.Add(predicate);
        return this;
    }

    /// <summary>Convenience case-insensitive title filter (applied in-memory).</summary>
    public CalendarEventQuery TitleContains(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return this.Where(e => e.Title.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Sorts the results. Replaces any previously configured sort.</summary>
    public CalendarEventQuery OrderBy(CalendarEventSortField field, bool descending = false)
    {
        this.sorts.Clear();
        this.sorts.Add((field, descending));
        return this;
    }

    /// <summary>Adds a secondary sort. Must follow <see cref="OrderBy"/>.</summary>
    public CalendarEventQuery ThenBy(CalendarEventSortField field, bool descending = false)
    {
        if (this.sorts.Count == 0)
            throw new InvalidOperationException($"{nameof(ThenBy)} requires a preceding {nameof(OrderBy)}.");

        this.sorts.Add((field, descending));
        return this;
    }

    /// <summary>Skips the first <paramref name="count"/> results.</summary>
    public CalendarEventQuery Skip(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        this.descriptor.Skip = count;
        return this;
    }

    /// <summary>Limits the result to <paramref name="count"/> events.</summary>
    public CalendarEventQuery Take(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        this.descriptor.Take = count;
        return this;
    }

    /// <summary>Runs the query and returns all matching events.</summary>
    public async Task<IReadOnlyList<CalendarEvent>> ToListAsync(CancellationToken ct = default)
        => await this.Execute(ct).ConfigureAwait(false);

    /// <summary>Runs the query and returns the first matching event, or null.</summary>
    public async Task<CalendarEvent?> FirstOrDefaultAsync(CancellationToken ct = default)
    {
        var list = await this.Execute(ct).ConfigureAwait(false);
        return list.Count == 0 ? null : list[0];
    }

    /// <summary>Runs the query and returns the number of matching events.</summary>
    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        var list = await this.Execute(ct).ConfigureAwait(false);
        return list.Count;
    }

    async Task<List<CalendarEvent>> Execute(CancellationToken ct)
    {
        var fetched = await this.executor(this.descriptor, ct).ConfigureAwait(false);
        IEnumerable<CalendarEvent> query = fetched;

        foreach (var predicate in this.predicates)
            query = query.Where(predicate);

        if (this.sorts.Count > 0)
            query = this.ApplySort(query);

        if (this.descriptor.Skip is { } skip)
            query = query.Skip(skip);

        if (this.descriptor.Take is { } take)
            query = query.Take(take);

        return query.ToList();
    }

    IEnumerable<CalendarEvent> ApplySort(IEnumerable<CalendarEvent> query)
    {
        var (field, descending) = this.sorts[0];
        var ordered = descending
            ? query.OrderByDescending(e => SortKey(e, field))
            : query.OrderBy(e => SortKey(e, field));

        for (var i = 1; i < this.sorts.Count; i++)
        {
            var (thenField, thenDescending) = this.sorts[i];
            ordered = thenDescending
                ? ordered.ThenByDescending(e => SortKey(e, thenField))
                : ordered.ThenBy(e => SortKey(e, thenField));
        }
        return ordered;
    }

    static IComparable SortKey(CalendarEvent calendarEvent, CalendarEventSortField field) => field switch
    {
        CalendarEventSortField.Start => calendarEvent.Start,
        CalendarEventSortField.End => calendarEvent.End,
        CalendarEventSortField.Title => calendarEvent.Title,
        _ => calendarEvent.Start
    };
}
