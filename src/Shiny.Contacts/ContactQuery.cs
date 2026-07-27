using Shiny.Contacts.Internals;

namespace Shiny.Contacts;

/// <summary>
/// A contact field a <see cref="ContactQuery"/> filter can target. Name fields, phone numbers and
/// email addresses are pushed down to the native query on Android; everything else is filtered
/// in-memory after the fetch.
/// </summary>
public enum ContactField
{
    GivenName,
    FamilyName,
    MiddleName,
    NamePrefix,
    NameSuffix,
    Nickname,
    DisplayName,
    Note,
    Company,
    JobTitle,
    Department,

    /// <summary>Matches if ANY of the contact's phone numbers match.</summary>
    Phone,

    /// <summary>Matches if ANY of the contact's email addresses match.</summary>
    Email
}

/// <summary>
/// How a field filter compares its value. All comparisons are case-insensitive.
/// </summary>
public enum ContactFilterOperation
{
    Contains,
    StartsWith,
    EndsWith,
    Equals
}

/// <summary>
/// Whether a query's field filters must all match, or any one of them.
/// </summary>
public enum ContactFilterMatch
{
    /// <summary>Every field filter must match (AND). This is the default.</summary>
    All,

    /// <summary>At least one field filter must match (OR).</summary>
    Any
}

/// <summary>Fields a <see cref="ContactQuery"/> result can be sorted by.</summary>
public enum ContactSortField
{
    GivenName,
    FamilyName,
    DisplayName,
    Company
}

/// <summary>
/// Fluent, async query builder returned by <see cref="IContactStore.Query"/>. Nothing runs until you
/// call <see cref="ToListAsync"/>, <see cref="FirstOrDefaultAsync"/> or <see cref="CountAsync"/>, and
/// the native read happens off the calling thread - it is safe to await from the UI thread.
/// </summary>
/// <example>
/// <code>
/// var results = await contactStore
///     .Query()
///     .Search("smith")
///     .OrderBy(ContactSortField.FamilyName)
///     .ThenBy(ContactSortField.GivenName)
///     .Take(50)
///     .ToListAsync(ct);
/// </code>
/// </example>
public sealed class ContactQuery
{
    readonly Func<ContactQueryDescriptor, CancellationToken, Task<IEnumerable<Contact>>> executor;
    readonly ContactQueryDescriptor descriptor = new();
    readonly List<Func<Contact, bool>> predicates = [];
    readonly List<(ContactSortField Field, bool Descending)> sorts = [];

    /// <summary>
    /// Platform stores construct this with an executor that performs the native fetch for a
    /// <see cref="ContactQueryDescriptor"/>. App code obtains one from <see cref="IContactStore.Query"/>.
    /// </summary>
    public ContactQuery(Func<ContactQueryDescriptor, CancellationToken, Task<IEnumerable<Contact>>> executor)
        => this.executor = executor;

    /// <summary>
    /// Adds a field filter. Multiple filters combine per <see cref="Match"/> (AND by default).
    /// </summary>
    public ContactQuery Where(ContactField field, string value, ContactFilterOperation operation = ContactFilterOperation.Contains)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        this.descriptor.Filters.Add(new ContactQueryFilter
        {
            Field = field,
            Value = value,
            Operation = operation
        });
        return this;
    }

    /// <summary>
    /// Adds an arbitrary in-memory filter, applied after the native fetch. Multiple predicates are ANDed.
    /// Use this for anything the field filters don't cover (addresses, dates, relationships, …).
    /// </summary>
    public ContactQuery Where(Func<Contact, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        this.predicates.Add(predicate);
        return this;
    }

    /// <summary>
    /// Convenience "search box" filter - matches the text against given name, family name, display
    /// name, phone numbers and email addresses. Sets <see cref="Match"/> to
    /// <see cref="ContactFilterMatch.Any"/>.
    /// </summary>
    public ContactQuery Search(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        this.Match(ContactFilterMatch.Any);
        return this
            .Where(ContactField.GivenName, text)
            .Where(ContactField.FamilyName, text)
            .Where(ContactField.DisplayName, text)
            .Where(ContactField.Phone, text)
            .Where(ContactField.Email, text);
    }

    /// <summary>
    /// Sets whether the field filters must all match (default) or any one of them.
    /// </summary>
    public ContactQuery Match(ContactFilterMatch match)
    {
        this.descriptor.Match = match;
        return this;
    }

    /// <summary>Sorts the results. Replaces any previously configured sort.</summary>
    public ContactQuery OrderBy(ContactSortField field, bool descending = false)
    {
        this.sorts.Clear();
        this.sorts.Add((field, descending));
        return this;
    }

    /// <summary>Adds a secondary sort. Must follow <see cref="OrderBy"/>.</summary>
    public ContactQuery ThenBy(ContactSortField field, bool descending = false)
    {
        if (this.sorts.Count == 0)
            throw new InvalidOperationException($"{nameof(ThenBy)} requires a preceding {nameof(OrderBy)}.");

        this.sorts.Add((field, descending));
        return this;
    }

    /// <summary>Skips the first <paramref name="count"/> results.</summary>
    public ContactQuery Skip(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        this.descriptor.Skip = count;
        return this;
    }

    /// <summary>Limits the result to <paramref name="count"/> contacts.</summary>
    public ContactQuery Take(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        this.descriptor.Take = count;
        return this;
    }

    /// <summary>Runs the query and returns all matching contacts.</summary>
    public async Task<IReadOnlyList<Contact>> ToListAsync(CancellationToken ct = default)
        => await this.Execute(ct).ConfigureAwait(false);

    /// <summary>Runs the query and returns the first matching contact, or null.</summary>
    public async Task<Contact?> FirstOrDefaultAsync(CancellationToken ct = default)
    {
        var list = await this.Execute(ct).ConfigureAwait(false);
        return list.Count == 0 ? null : list[0];
    }

    /// <summary>Runs the query and returns the number of matching contacts.</summary>
    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        var list = await this.Execute(ct).ConfigureAwait(false);
        return list.Count;
    }

    async Task<List<Contact>> Execute(CancellationToken ct)
    {
        var fetched = await this.executor(this.descriptor, ct).ConfigureAwait(false);
        IEnumerable<Contact> query = fetched;

        // Field filters are always re-applied here: platforms translate only a subset of them natively
        // and fall back to a full read for the rest, so this is what guarantees the filter actually took.
        if (this.descriptor.Filters.Count > 0)
            query = query.Where(this.MatchesFilters);

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

    bool MatchesFilters(Contact contact)
    {
        if (this.descriptor.Match == ContactFilterMatch.Any)
            return this.descriptor.Filters.Any(f => Matches(contact, f));

        return this.descriptor.Filters.All(f => Matches(contact, f));
    }

    static bool Matches(Contact contact, ContactQueryFilter filter)
        => GetValues(contact, filter.Field).Any(v => Matches(v, filter.Operation, filter.Value));

    static bool Matches(string? value, ContactFilterOperation operation, string filterValue)
    {
        if (String.IsNullOrEmpty(value))
            return false;

        const StringComparison cmp = StringComparison.OrdinalIgnoreCase;
        return operation switch
        {
            ContactFilterOperation.Contains => value.Contains(filterValue, cmp),
            ContactFilterOperation.StartsWith => value.StartsWith(filterValue, cmp),
            ContactFilterOperation.EndsWith => value.EndsWith(filterValue, cmp),
            ContactFilterOperation.Equals => value.Equals(filterValue, cmp),
            _ => false
        };
    }

    static IEnumerable<string?> GetValues(Contact contact, ContactField field) => field switch
    {
        ContactField.GivenName => new string?[] { contact.GivenName },
        ContactField.FamilyName => new string?[] { contact.FamilyName },
        ContactField.MiddleName => new string?[] { contact.MiddleName },
        ContactField.NamePrefix => new string?[] { contact.NamePrefix },
        ContactField.NameSuffix => new string?[] { contact.NameSuffix },
        ContactField.Nickname => new string?[] { contact.Nickname },
        ContactField.DisplayName => new string?[] { contact.DisplayName },
        ContactField.Note => new string?[] { contact.Note },
        ContactField.Company => new string?[] { contact.Organization?.Company },
        ContactField.JobTitle => new string?[] { contact.Organization?.Title },
        ContactField.Department => new string?[] { contact.Organization?.Department },
        ContactField.Phone => contact.Phones.Select(x => (string?)x.Number),
        ContactField.Email => contact.Emails.Select(x => (string?)x.Address),
        _ => []
    };

    IEnumerable<Contact> ApplySort(IEnumerable<Contact> query)
    {
        var (field, descending) = this.sorts[0];
        var ordered = descending
            ? query.OrderByDescending(c => SortKey(c, field), StringComparer.OrdinalIgnoreCase)
            : query.OrderBy(c => SortKey(c, field), StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i < this.sorts.Count; i++)
        {
            var (thenField, thenDescending) = this.sorts[i];
            ordered = thenDescending
                ? ordered.ThenByDescending(c => SortKey(c, thenField), StringComparer.OrdinalIgnoreCase)
                : ordered.ThenBy(c => SortKey(c, thenField), StringComparer.OrdinalIgnoreCase);
        }
        return ordered;
    }

    static string SortKey(Contact contact, ContactSortField field) => field switch
    {
        ContactSortField.GivenName => contact.GivenName ?? String.Empty,
        ContactSortField.FamilyName => contact.FamilyName ?? String.Empty,
        ContactSortField.DisplayName => contact.DisplayName ?? String.Empty,
        ContactSortField.Company => contact.Organization?.Company ?? String.Empty,
        _ => String.Empty
    };
}
