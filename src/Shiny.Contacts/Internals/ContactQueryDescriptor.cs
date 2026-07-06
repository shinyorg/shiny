using System.Linq.Expressions;

namespace Shiny.Contacts.Internals;

/// <summary>
/// Represents a parsed filter extracted from a LINQ expression tree.
/// Platform implementations inspect these to build native queries.
/// </summary>
public class ContactQueryFilter
{
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// For collection properties (Phones, Emails, etc.), the sub-property being filtered.
    /// e.g. Phones → Number, Emails → Address
    /// </summary>
    public string? SubPropertyName { get; set; }

    public ContactFilterOperation Operation { get; set; }
    public string Value { get; set; } = string.Empty;
}

public enum ContactFilterOperation
{
    Contains,
    StartsWith,
    EndsWith,
    Equals
}

/// <summary>
/// Holds the parsed query information extracted from an IQueryable expression.
/// </summary>
public class ContactQueryDescriptor
{
    public List<ContactQueryFilter> Filters { get; } = [];

    /// <summary>
    /// Any predicate portions that couldn't be translated to native filters.
    /// Applied as in-memory post-filters.
    /// </summary>
    public Expression<Func<Contact, bool>>? InMemoryPredicate { get; set; }

    public int? Skip { get; set; }
    public int? Take { get; set; }
}
