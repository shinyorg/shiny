namespace Sample.Shared.Maui.Services;

public record EventRecord(
    long Id,
    string Category,
    string Description,
    string? Metadata,
    DateTimeOffset Timestamp
);

public interface IEventStore
{
    Task Add(string category, string description, IDictionary<string, string?>? metadata = null, CancellationToken ct = default);
    Task<IReadOnlyList<EventRecord>> GetAll(string? category = null, int limit = 200, CancellationToken ct = default);
    Task Clear(CancellationToken ct = default);
    event EventHandler<EventRecord>? EventAdded;
}
