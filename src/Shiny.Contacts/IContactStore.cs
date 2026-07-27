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
    /// Starts a fluent contact query. Configure it with <c>Where</c>/<c>Search</c>/<c>OrderBy</c>/
    /// <c>Skip</c>/<c>Take</c>, then run it with <c>ToListAsync(ct)</c>. Field filters are translated
    /// to native queries where the platform supports it, and always re-applied in-memory. The native
    /// read runs off the calling thread, so it is safe to await from the UI thread.
    /// </summary>
    /// <example>
    /// <code>
    /// var results = await store.Query().Search("smith").Take(50).ToListAsync(ct);
    /// </code>
    /// </example>
    ContactQuery Query();

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
