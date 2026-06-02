using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Sample.Shared.Maui.Services;

public sealed class SqliteEventStore : IEventStore, IAsyncDisposable
{
    readonly SemaphoreSlim sync = new(1, 1);
    readonly ILogger<SqliteEventStore> logger;
    readonly string connectionString;
    bool initialized;

    public SqliteEventStore(ILogger<SqliteEventStore> logger)
    {
        this.logger = logger;
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "sample-events.db3");
        this.connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public event EventHandler<EventRecord>? EventAdded;

    public async Task Add(string category, string description, IDictionary<string, string?>? metadata = null, CancellationToken ct = default)
    {
        await this.EnsureInit(ct);
        var ts = DateTimeOffset.UtcNow;
        var metaJson = metadata is { Count: > 0 } ? JsonSerializer.Serialize(metadata) : null;

        long id;
        await this.sync.WaitAsync(ct);
        try
        {
            await using var conn = new SqliteConnection(this.connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO events (category, description, metadata, timestamp)
                VALUES ($cat, $desc, $meta, $ts);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("$cat", category);
            cmd.Parameters.AddWithValue("$desc", description);
            cmd.Parameters.AddWithValue("$meta", (object?)metaJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$ts", ts.ToUnixTimeMilliseconds());
            id = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);
        }
        finally
        {
            this.sync.Release();
        }

        var record = new EventRecord(id, category, description, metaJson, ts);
        try { this.EventAdded?.Invoke(this, record); }
        catch (Exception ex) { this.logger.LogWarning(ex, "EventAdded handler threw"); }
    }

    public async Task<IReadOnlyList<EventRecord>> GetAll(string? category = null, int limit = 200, CancellationToken ct = default)
    {
        await this.EnsureInit(ct);

        await this.sync.WaitAsync(ct);
        try
        {
            await using var conn = new SqliteConnection(this.connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            if (category is null)
            {
                cmd.CommandText = "SELECT id, category, description, metadata, timestamp FROM events ORDER BY id DESC LIMIT $limit";
            }
            else
            {
                cmd.CommandText = "SELECT id, category, description, metadata, timestamp FROM events WHERE category = $cat ORDER BY id DESC LIMIT $limit";
                cmd.Parameters.AddWithValue("$cat", category);
            }
            cmd.Parameters.AddWithValue("$limit", limit);

            var list = new List<EventRecord>(capacity: Math.Min(limit, 64));
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new EventRecord(
                    Id: reader.GetInt64(0),
                    Category: reader.GetString(1),
                    Description: reader.GetString(2),
                    Metadata: reader.IsDBNull(3) ? null : reader.GetString(3),
                    Timestamp: DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(4))
                ));
            }
            return list;
        }
        finally
        {
            this.sync.Release();
        }
    }

    public async Task Clear(CancellationToken ct = default)
    {
        await this.EnsureInit(ct);
        await this.sync.WaitAsync(ct);
        try
        {
            await using var conn = new SqliteConnection(this.connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM events";
            await cmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            this.sync.Release();
        }
    }

    async Task EnsureInit(CancellationToken ct)
    {
        if (this.initialized)
            return;

        await this.sync.WaitAsync(ct);
        try
        {
            if (this.initialized)
                return;

            await using var conn = new SqliteConnection(this.connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS events (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    category TEXT NOT NULL,
                    description TEXT NOT NULL,
                    metadata TEXT,
                    timestamp INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_events_category ON events(category);
                CREATE INDEX IF NOT EXISTS idx_events_ts ON events(timestamp DESC);
                """;
            await cmd.ExecuteNonQueryAsync(ct);
            this.initialized = true;
        }
        finally
        {
            this.sync.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        this.sync.Dispose();
        return ValueTask.CompletedTask;
    }
}
