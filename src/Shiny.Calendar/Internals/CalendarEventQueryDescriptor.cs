using System.Linq.Expressions;

namespace Shiny.Calendar.Internals;

/// <summary>
/// Holds the parsed query information extracted from an <see cref="IQueryable{CalendarEvent}"/> expression.
/// Platform implementations map <see cref="CalendarId"/> / <see cref="StartAfter"/> / <see cref="EndBefore"/>
/// onto their native fetch; the remainder is applied in-memory.
/// </summary>
public class CalendarEventQueryDescriptor
{
    /// <summary>Native filter: only events on this calendar (from <c>e.CalendarId == "..."</c>).</summary>
    public string? CalendarId { get; set; }

    /// <summary>Native filter: lower bound of the event window (from <c>e.Start &gt;= d</c> / <c>e.End &gt;= d</c>).</summary>
    public DateTimeOffset? StartAfter { get; set; }

    /// <summary>Native filter: upper bound of the event window (from <c>e.End &lt;= d</c> / <c>e.Start &lt;= d</c>).</summary>
    public DateTimeOffset? EndBefore { get; set; }

    /// <summary>
    /// Any predicate portions that couldn't be translated to native filters.
    /// Applied as in-memory post-filters.
    /// </summary>
    public Expression<Func<CalendarEvent, bool>>? InMemoryPredicate { get; set; }

    public int? Skip { get; set; }
    public int? Take { get; set; }
}
