namespace Shiny.CallDirectory.Infrastructure;


/// <summary>
/// The SQLite file the app writes and the Call Directory extension reads. The schema is a contract with
/// <c>buildTransitive/swift/ShinyCallDirectoryHandler.swift</c> - change one, change both, and bump
/// <see cref="SchemaVersion"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>entries</c> is the list as it should be.</item>
/// <item><c>changes</c> logs every number that changed, with its type before the change (NULL = not stored), so an
/// incremental load knows what iOS currently holds without the extension ever writing to this file.</item>
/// <item><c>meta.generation</c> is bumped by <see cref="ReplaceAll"/> and <see cref="Clear"/>; a generation the
/// extension has not applied forces a full load.</item>
/// </list>
/// Every call opens, works in one transaction and closes. A suspended app holding a lock on a file in a shared
/// container is killed by iOS (0xdead10cc), so no connection outlives a call. The journal is the rollback kind
/// for the same reason - WAL keeps -shm/-wal files mapped and locked by every open connection.
/// <para>
/// <c>trackChanges</c> is false on Android: its call screening reads <c>entries</c> live and never consumes the log.
/// </para>
/// </remarks>
class CallDirectoryStore(string path, bool trackChanges = true)
{
    public const int SchemaVersion = 1;
    readonly SemaphoreSlim gate = new(1, 1);

    public string Path => path;


    public Task Set(IEnumerable<CallDirectoryEntry> entries, CancellationToken cancelToken) => this.Write(db =>
    {
        using var get = db.Prepare("SELECT type, label FROM entries WHERE number = ?1");
        using var log = db.Prepare("INSERT INTO changes (number, old_type) VALUES (?1, ?2)");
        using var upsert = db.Prepare("INSERT INTO entries (number, label, type) VALUES (?1, ?2, ?3) ON CONFLICT(number) DO UPDATE SET label = excluded.label, type = excluded.type");

        foreach (var entry in entries)
        {
            cancelToken.ThrowIfCancellationRequested();
            Validate(entry);

            long? oldType = null;
            string? oldLabel = null;
            get.Bind(1, entry.PhoneNumber);
            if (get.StepRow())
            {
                oldType = get.GetInt64(0);
                oldLabel = get.GetText(1);
            }
            get.ResetStatement();

            // unchanged rows stay out of the log, so re-sending a whole list only reloads what differs
            if (oldType != (long)entry.Type || oldLabel != entry.Label)
            {
                if (trackChanges)
                    LogChange(log, entry.PhoneNumber, oldType);
                upsert.Bind(1, entry.PhoneNumber).Bind(2, entry.Label).Bind(3, (long)entry.Type).StepDone();
            }
        }
    }, cancelToken);


    public Task Remove(IEnumerable<long> phoneNumbers, CancellationToken cancelToken) => this.Write(db =>
    {
        using var get = db.Prepare("SELECT type FROM entries WHERE number = ?1");
        using var log = db.Prepare("INSERT INTO changes (number, old_type) VALUES (?1, ?2)");
        using var delete = db.Prepare("DELETE FROM entries WHERE number = ?1");

        foreach (var number in phoneNumbers)
        {
            cancelToken.ThrowIfCancellationRequested();
            get.Bind(1, number);
            var exists = get.StepRow();
            var oldType = exists ? get.GetInt64(0) : 0;
            get.ResetStatement();

            // an unknown number never reaches the log - iOS fails a load that removes what it never had
            if (exists)
            {
                if (trackChanges)
                    LogChange(log, number, oldType);
                delete.Bind(1, number).StepDone();
            }
        }
    }, cancelToken);


    public Task ReplaceAll(IEnumerable<CallDirectoryEntry> entries, CancellationToken cancelToken) => this.Write(db =>
    {
        db.Execute("DELETE FROM entries");
        db.Execute("DELETE FROM changes");
        db.Execute("UPDATE meta SET value = CAST(value AS INTEGER) + 1 WHERE key = 'generation'");

        using var insert = db.Prepare("INSERT OR REPLACE INTO entries (number, label, type) VALUES (?1, ?2, ?3)");
        foreach (var entry in entries)
        {
            cancelToken.ThrowIfCancellationRequested();
            Validate(entry);
            insert.Bind(1, entry.PhoneNumber).Bind(2, entry.Label).Bind(3, (long)entry.Type).StepDone();
        }
    }, cancelToken);


    public Task Clear(CancellationToken cancelToken) => this.ReplaceAll([], cancelToken);


    /// <summary>
    /// Drops the change rows the extension has applied. Rows of another generation are already gone, so a
    /// mismatched generation trims nothing.
    /// </summary>
    public Task Trim(long appliedGeneration, long appliedSeq, CancellationToken cancelToken) => this.Write(db =>
    {
        using var gen = db.Prepare("SELECT CAST(value AS INTEGER) FROM meta WHERE key = 'generation'");
        gen.StepRow();
        if (gen.GetInt64(0) == appliedGeneration)
        {
            using var delete = db.Prepare("DELETE FROM changes WHERE seq <= ?1");
            delete.Bind(1, appliedSeq).StepDone();
        }
    }, cancelToken);


    public Task<CallDirectoryEntry?> Get(long phoneNumber, CancellationToken cancelToken) => this.Read(db =>
    {
        using var get = db.Prepare("SELECT label, type FROM entries WHERE number = ?1");
        get.Bind(1, phoneNumber);
        return get.StepRow()
            ? new CallDirectoryEntry(phoneNumber, get.GetText(0), (CallDirectoryEntryType)get.GetInt64(1))
            : null;
    }, cancelToken);


    public Task<int> Count(CancellationToken cancelToken) => this.Read(db => (int)Scalar(db, "SELECT COUNT(*) FROM entries"), cancelToken);


    /// <summary>The number of rows in the change log - for tests and diagnostics.</summary>
    public Task<int> PendingChanges(CancellationToken cancelToken) => this.Read(db => (int)Scalar(db, "SELECT COUNT(*) FROM changes"), cancelToken);


    public Task<long> Generation(CancellationToken cancelToken) => this.Read(db => Scalar(db, "SELECT CAST(value AS INTEGER) FROM meta WHERE key = 'generation'"), cancelToken);


    static long Scalar(Sqlite.Connection db, string sql)
    {
        using var stmt = db.Prepare(sql);
        stmt.StepRow();
        return stmt.GetInt64(0);
    }


    static void LogChange(Sqlite.Statement log, long number, long? oldType)
    {
        log.Bind(1, number);
        if (oldType == null)
            log.Bind(2, (string?)null);
        else
            log.Bind(2, oldType.Value);
        log.StepDone();
    }


    static void Validate(CallDirectoryEntry entry)
    {
        if (entry.PhoneNumber <= 0)
            throw new ArgumentException($"{entry.PhoneNumber} is not a phone number - use PhoneNumbers.ToCallDirectory");

        if (!Enum.IsDefined(entry.Type))
            throw new ArgumentException($"Unknown entry type {entry.Type}");
    }


    Task Write(Action<Sqlite.Connection> work, CancellationToken cancelToken) => this.Read<bool>(db =>
    {
        // IMMEDIATE takes the write lock up front, so a read-then-write never deadlocks against the extension
        db.Execute("BEGIN IMMEDIATE");
        try
        {
            work(db);
            db.Execute("COMMIT");
        }
        catch
        {
            db.Execute("ROLLBACK");
            throw;
        }
        return true;
    }, cancelToken);


    async Task<T> Read<T>(Func<Sqlite.Connection, T> work, CancellationToken cancelToken)
    {
        await this.gate.WaitAsync(cancelToken).ConfigureAwait(false);
        try
        {
            // sqlite is blocking I/O; keep it off the caller's (often the UI) thread
            return await Task.Run(() =>
            {
                using var db = new Sqlite.Connection(path);
                EnsureSchema(db);
                return work(db);
            }, cancelToken).ConfigureAwait(false);
        }
        finally
        {
            this.gate.Release();
        }
    }


    static void EnsureSchema(Sqlite.Connection db)
    {
        db.Execute("PRAGMA journal_mode = DELETE");
        db.Execute("CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT NOT NULL)");
        db.Execute("CREATE TABLE IF NOT EXISTS entries (number INTEGER PRIMARY KEY, label TEXT, type INTEGER NOT NULL)");
        db.Execute("CREATE TABLE IF NOT EXISTS changes (seq INTEGER PRIMARY KEY AUTOINCREMENT, number INTEGER NOT NULL, old_type INTEGER)");
        db.Execute($"INSERT OR IGNORE INTO meta (key, value) VALUES ('schema_version', '{SchemaVersion}')");
        db.Execute("INSERT OR IGNORE INTO meta (key, value) VALUES ('generation', '1')");

        using var version = db.Prepare("SELECT CAST(value AS INTEGER) FROM meta WHERE key = 'schema_version'");
        version.StepRow();
        var found = version.GetInt64(0);
        if (found != SchemaVersion)
            throw new InvalidOperationException($"The call directory store is schema version {found}; this version of Shiny.Mobile.CallDirectory reads {SchemaVersion}");
    }
}
