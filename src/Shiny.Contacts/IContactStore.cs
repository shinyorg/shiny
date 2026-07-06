namespace Shiny.Contacts;

public interface IContactStore
{
    /// <summary>
    /// Returns the current contact access/permission state without prompting the user.
    /// </summary>
    AccessState GetCurrentAccess();

    /// <summary>
    /// Requests contact access from the user (prompting if necessary) and returns the resulting state.
    /// </summary>
    Task<AccessState> RequestAccess(CancellationToken ct = default);

    /// <summary>
    /// Retrieves all contacts from the device.
    /// </summary>
    Task<IReadOnlyList<Contact>> GetAll(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single contact by its platform identifier.
    /// </summary>
    Task<Contact?> GetById(string contactId, CancellationToken ct = default);

    /// <summary>
    /// Returns a LINQ-queryable source of contacts.
    /// Supports .Where() with .Contains(), .StartsWith(), .Equals() on string properties.
    /// Predicates are translated to native queries where possible, with in-memory fallback.
    /// </summary>
    IQueryable<Contact> Query();

    /// <summary>
    /// Creates a new contact and returns the platform-assigned identifier.
    /// </summary>
    Task<string> Create(Contact contact, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing contact. The contact must have a valid Id.
    /// </summary>
    Task Update(Contact contact, CancellationToken ct = default);

    /// <summary>
    /// Deletes the contact with the specified identifier.
    /// </summary>
    Task Delete(string contactId, CancellationToken ct = default);
}
