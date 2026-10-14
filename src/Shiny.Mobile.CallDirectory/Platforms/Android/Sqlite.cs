using Android.Database;
using Android.Database.Sqlite;

namespace Shiny.CallDirectory.Infrastructure;


/// <summary>
/// The same surface as the Apple <c>Sqlite</c> wrapper, over Android.Database.Sqlite - apps cannot load the
/// platform's private libsqlite directly. <see cref="CallDirectoryStore"/> runs unchanged on top of it.
/// </summary>
/// <remarks>
/// Android's API is cursor + bind-args rather than prepare/step, so a <see cref="Statement"/> only collects its
/// arguments: <see cref="Statement.StepRow"/> runs it as a query, <see cref="Statement.StepDone"/> as a command.
/// Transaction and PRAGMA statements are routed to the SQLiteDatabase methods meant for them.
/// </remarks>
static class Sqlite
{
    public sealed class Connection : IDisposable
    {
        readonly SQLiteDatabase db;

        public Connection(string path)
        {
            try
            {
                this.db = SQLiteDatabase.OpenDatabase(path, null, DatabaseOpenFlags.CreateIfNecessary | DatabaseOpenFlags.OpenReadwrite)
                    ?? throw new InvalidOperationException($"Could not open call directory store '{path}'");
            }
            catch (SQLiteException ex)
            {
                throw new InvalidOperationException($"Could not open call directory store '{path}': {ex.Message}", ex);
            }
        }

        public void Execute(string sql)
        {
            var trimmed = sql.TrimStart();
            if (trimmed.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase))
            {
                this.db.BeginTransaction();
            }
            else if (trimmed.Equals("COMMIT", StringComparison.OrdinalIgnoreCase))
            {
                this.db.SetTransactionSuccessful();
                this.db.EndTransaction();
            }
            else if (trimmed.Equals("ROLLBACK", StringComparison.OrdinalIgnoreCase))
            {
                this.db.EndTransaction();
            }
            else if (trimmed.StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase))
            {
                // a PRAGMA returns a row, which execSQL refuses
                using var cursor = this.db.RawQuery(sql, null);
                cursor?.MoveToFirst();
            }
            else
            {
                this.db.ExecSQL(sql);
            }
        }

        public Statement Prepare(string sql) => new(this.db, sql);

        public void Dispose() => this.db.Close();
    }


    public sealed class Statement(SQLiteDatabase db, string sql) : IDisposable
    {
        readonly List<object?> args = [];
        ICursor? cursor;

        public Statement Bind(int index, long value) => this.Set(index, value);
        public Statement Bind(int index, string? value) => this.Set(index, value);

        Statement Set(int index, object? value)
        {
            // parameters are ?1..?n, bound in order
            while (this.args.Count < index)
                this.args.Add(null);

            this.args[index - 1] = value;
            return this;
        }

        /// <summary>True while a row is available.</summary>
        public bool StepRow()
        {
            // rawQuery binds strings only - INTEGER-affinity columns convert them back for the comparison
            this.cursor ??= db.RawQuery(sql, this.args.Select(x => x?.ToString()).ToArray()!);
            return this.cursor?.MoveToNext() ?? false;
        }

        public void StepDone()
        {
            // the store only ever binds long, string or null
            var bound = this.args
                .Select(x => x switch
                {
                    long l => Java.Lang.Long.ValueOf(l),
                    string str => (Java.Lang.Object)new Java.Lang.String(str),
                    _ => null
                })
                .ToArray();

            db.ExecSQL(sql, bound!);
            this.ResetStatement();
        }

        public void ResetStatement()
        {
            this.cursor?.Close();
            this.cursor = null;
            this.args.Clear();
        }

        public long GetInt64(int col) => this.cursor!.GetLong(col);
        public bool IsNull(int col) => this.cursor!.IsNull(col);
        public string? GetText(int col) => this.IsNull(col) ? null : this.cursor!.GetString(col);

        public void Dispose() => this.cursor?.Close();
    }
}
