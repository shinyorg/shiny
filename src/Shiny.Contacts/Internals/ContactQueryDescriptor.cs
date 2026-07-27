namespace Shiny.Contacts.Internals;

/// <summary>
/// A single field filter configured on a <see cref="ContactQuery"/>.
/// Platform implementations inspect these to build native queries.
/// </summary>
public class ContactQueryFilter
{
    public ContactField Field { get; init; }
    public ContactFilterOperation Operation { get; init; }
    public string Value { get; init; } = String.Empty;
}

/// <summary>
/// The fetch instructions a <see cref="ContactQuery"/> hands to the platform store.
/// <para>
/// Everything here is a HINT used to narrow the native read. The builder re-applies every filter
/// (plus skip/take/sort) in-memory afterwards, so an implementation is always free to translate only
/// the parts it can and return a superset - it must never return fewer contacts than the hints allow.
/// </para>
/// </summary>
public class ContactQueryDescriptor
{
    public List<ContactQueryFilter> Filters { get; } = [];

    /// <summary>Whether <see cref="Filters"/> should be intersected (All) or unioned (Any).</summary>
    public ContactFilterMatch Match { get; set; }

    public int? Skip { get; set; }
    public int? Take { get; set; }
}
