using System.Collections;
using System.Linq.Expressions;

namespace Shiny.Calendar.Internals;

/// <summary>
/// IQueryable implementation backed by a custom provider that extracts
/// native-translatable filters from the expression tree.
/// </summary>
public class CalendarEventQueryable : IQueryable<CalendarEvent>, IOrderedQueryable<CalendarEvent>
{
    readonly CalendarEventQueryProvider provider;
    readonly Expression expression;

    public CalendarEventQueryable(CalendarEventQueryProvider provider)
    {
        this.provider = provider;
        this.expression = Expression.Constant(this);
    }

    public CalendarEventQueryable(CalendarEventQueryProvider provider, Expression expression)
    {
        this.provider = provider;
        this.expression = expression;
    }

    public Type ElementType => typeof(CalendarEvent);
    public Expression Expression => this.expression;
    public IQueryProvider Provider => this.provider;

    public IEnumerator<CalendarEvent> GetEnumerator() => this.provider.Execute<IEnumerable<CalendarEvent>>(this.expression).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

public class CalendarEventQueryProvider(Func<CalendarEventQueryDescriptor, IEnumerable<CalendarEvent>> executor) : IQueryProvider
{
    public IQueryable CreateQuery(Expression expression)
        => new CalendarEventQueryable(this, expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        if (typeof(TElement) != typeof(CalendarEvent))
            throw new NotSupportedException($"Only IQueryable<CalendarEvent> is supported, not IQueryable<{typeof(TElement).Name}>");
        return (IQueryable<TElement>)(object)new CalendarEventQueryable(this, expression);
    }

    public object? Execute(Expression expression)
        => this.Execute<IEnumerable<CalendarEvent>>(expression);

    public TResult Execute<TResult>(Expression expression)
    {
        var descriptor = CalendarExpressionVisitor.Parse(expression);
        var results = executor(descriptor);

        if (descriptor.InMemoryPredicate != null)
        {
            var predicate = descriptor.InMemoryPredicate;
            results = results.Where(e => CalendarExpressionInterpreter.Evaluate(predicate, e));
        }

        if (descriptor.Skip.HasValue)
            results = results.Skip(descriptor.Skip.Value);

        if (descriptor.Take.HasValue)
            results = results.Take(descriptor.Take.Value);

        if (typeof(TResult) == typeof(IEnumerable<CalendarEvent>))
            return (TResult)results;

        // Support single-element operations (First, Count, etc.)
        var list = results.ToList();
        return typeof(TResult).Name switch
        {
            nameof(Int32) => (TResult)(object)list.Count,
            _ => (TResult)(object)list
        };
    }
}
