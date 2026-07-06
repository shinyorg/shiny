using System.Collections;
using System.Linq.Expressions;

namespace Shiny.Contacts.Internals;

/// <summary>
/// IQueryable implementation backed by a custom provider that extracts
/// native-translatable filters from the expression tree.
/// </summary>
public class ContactQueryable : IQueryable<Contact>, IOrderedQueryable<Contact>
{
    readonly ContactQueryProvider provider;
    readonly Expression expression;

    public ContactQueryable(ContactQueryProvider provider)
    {
        this.provider = provider;
        expression = Expression.Constant(this);
    }

    public ContactQueryable(ContactQueryProvider provider, Expression expression)
    {
        this.provider = provider;
        this.expression = expression;
    }

    public Type ElementType => typeof(Contact);
    public Expression Expression => expression;
    public IQueryProvider Provider => provider;

    public IEnumerator<Contact> GetEnumerator() => provider.Execute<IEnumerable<Contact>>(expression).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class ContactQueryProvider(Func<ContactQueryDescriptor, IEnumerable<Contact>> executor) : IQueryProvider
{
    public IQueryable CreateQuery(Expression expression)
        => new ContactQueryable(this, expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        if (typeof(TElement) != typeof(Contact))
            throw new NotSupportedException($"Only IQueryable<Contact> is supported, not IQueryable<{typeof(TElement).Name}>");
        return (IQueryable<TElement>)(object)new ContactQueryable(this, expression);
    }

    public object? Execute(Expression expression)
        => Execute<IEnumerable<Contact>>(expression);

    public TResult Execute<TResult>(Expression expression)
    {
        var descriptor = ContactExpressionVisitor.Parse(expression);
        var results = executor(descriptor);

        if (descriptor.InMemoryPredicate != null)
        {
            var predicate = descriptor.InMemoryPredicate;
            results = results.Where(c => ExpressionInterpreter.Evaluate(predicate, c));
        }

        if (descriptor.Skip.HasValue)
            results = results.Skip(descriptor.Skip.Value);

        if (descriptor.Take.HasValue)
            results = results.Take(descriptor.Take.Value);

        if (typeof(TResult) == typeof(IEnumerable<Contact>))
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
