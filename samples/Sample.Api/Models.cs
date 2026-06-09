using System.Text.Json;

namespace Sample.Api;


/// <summary>
/// Matches the shape of <c>TodoItem</c> the Shiny.Data.Sync client uses (<c>ISyncEntity</c> requires
/// <c>Identifier</c>). Stored in the document store using <c>Identifier</c> as its Id.
/// </summary>
public class TodoItem
{
    public string Identifier { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}


/// <summary>
/// Append-only change log row. The data-sync pull endpoint serves these in timestamp order,
/// using the <see cref="Timestamp"/> of the last row returned as the next cursor.
/// </summary>
public class SyncChange
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Verb { get; set; } = string.Empty;
    public string Payload { get; set; } = "null";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}


/// <summary>
/// Metadata for a file uploaded via the large-file-transfer endpoints.
/// </summary>
public class FileRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string DownloadUrl { get; set; } = string.Empty;
}


// ---- Data-sync wire types (match the Shiny.Data.Sync default REST shape exactly) ----

public record SyncPullItem(string Id, string Verb, JsonElement Payload);
public record SyncPullResponse(string? Cursor, bool HasMore, IReadOnlyList<SyncPullItem> Items);

public record SyncBatchOperation(string Id, string EntityId, string Verb, JsonElement? Payload);
public record SyncBatchRequest(IReadOnlyList<SyncBatchOperation> Operations);
public record SyncBatchResultItem(string Id, int Status, JsonElement? Body, string? Error);
public record SyncBatchResponse(IReadOnlyList<SyncBatchResultItem> Results);
