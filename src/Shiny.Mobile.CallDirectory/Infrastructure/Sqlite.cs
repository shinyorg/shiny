#if !ANDROID
using System.Runtime.InteropServices;

namespace Shiny.CallDirectory.Infrastructure;


/// <summary>
/// Just enough of the system libsqlite3 for <see cref="CallDirectoryStore"/> on Apple (and the macOS tests).
/// Android cannot load the platform's private libsqlite, so it has its own over Android.Database.Sqlite
/// (Platforms/Android/Sqlite.cs) with the same surface. Apple ships SQLite in every OS, and
/// the Swift extension uses the same library, so there is no SQLitePCLRaw bundle and nothing trim/AOT can break.
/// </summary>
static partial class Sqlite
{
    const string Lib = "/usr/lib/libsqlite3.dylib";

    public const int Ok = 0;
    public const int Row = 100;
    public const int Done = 101;
    public const int Null = 5;

    public const int OpenReadWrite = 0x00000002;
    public const int OpenCreate = 0x00000004;
    public const int OpenFullMutex = 0x00010000;

    // SQLITE_TRANSIENT - sqlite copies the bound text before the call returns
    static readonly nint Transient = -1;

    [LibraryImport(Lib, EntryPoint = "sqlite3_open_v2", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int OpenV2(string fileName, out nint db, int flags, nint vfs);

    [LibraryImport(Lib, EntryPoint = "sqlite3_close_v2")]
    private static partial int CloseV2(nint db);

    [LibraryImport(Lib, EntryPoint = "sqlite3_busy_timeout")]
    private static partial int BusyTimeout(nint db, int ms);

    [LibraryImport(Lib, EntryPoint = "sqlite3_errmsg")]
    private static partial nint ErrMsg(nint db);

    [LibraryImport(Lib, EntryPoint = "sqlite3_prepare_v2", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int PrepareV2(nint db, string sql, int byteCount, out nint stmt, nint tail);

    [LibraryImport(Lib, EntryPoint = "sqlite3_step")]
    private static partial int Step(nint stmt);

    [LibraryImport(Lib, EntryPoint = "sqlite3_reset")]
    private static partial int Reset(nint stmt);

    [LibraryImport(Lib, EntryPoint = "sqlite3_finalize")]
    private static partial int FinalizeStatement(nint stmt);

    [LibraryImport(Lib, EntryPoint = "sqlite3_bind_int64")]
    private static partial int BindInt64(nint stmt, int index, long value);

    [LibraryImport(Lib, EntryPoint = "sqlite3_bind_null")]
    private static partial int BindNull(nint stmt, int index);

    [LibraryImport(Lib, EntryPoint = "sqlite3_bind_text", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int BindText(nint stmt, int index, string value, int byteCount, nint destructor);

    [LibraryImport(Lib, EntryPoint = "sqlite3_column_int64")]
    private static partial long ColumnInt64(nint stmt, int index);

    [LibraryImport(Lib, EntryPoint = "sqlite3_column_text")]
    private static partial nint ColumnText(nint stmt, int index);

    [LibraryImport(Lib, EntryPoint = "sqlite3_column_type")]
    private static partial int ColumnType(nint stmt, int index);


    public sealed class Connection : IDisposable
    {
        nint handle;

        public Connection(string path)
        {
            var rc = OpenV2(path, out this.handle, OpenReadWrite | OpenCreate | OpenFullMutex, 0);
            if (rc != Ok)
            {
                var msg = this.handle == 0 ? $"sqlite error {rc}" : this.Error();
                CloseV2(this.handle);
                throw new InvalidOperationException($"Could not open call directory store '{path}': {msg}");
            }
            // the extension may be mid-load holding a read lock; wait for it rather than fail the write
            BusyTimeout(this.handle, 5000);
        }

        public void Execute(string sql)
        {
            using var stmt = this.Prepare(sql);
            stmt.StepDone();
        }

        public Statement Prepare(string sql)
        {
            var rc = PrepareV2(this.handle, sql, -1, out var stmt, 0);
            if (rc != Ok)
                throw new InvalidOperationException($"sqlite prepare failed ({rc}): {this.Error()} - {sql}");

            return new Statement(this, stmt);
        }

        internal string Error() => Marshal.PtrToStringUTF8(ErrMsg(this.handle)) ?? "unknown error";

        public void Dispose()
        {
            if (this.handle != 0)
            {
                CloseV2(this.handle);
                this.handle = 0;
            }
        }
    }


    public sealed class Statement(Connection conn, nint handle) : IDisposable
    {
        public Statement Bind(int index, long value)
        {
            BindInt64(handle, index, value);
            return this;
        }

        public Statement Bind(int index, string? value)
        {
            if (value == null)
                BindNull(handle, index);
            else
                BindText(handle, index, value, -1, Transient);

            return this;
        }

        /// <summary>True while a row is available.</summary>
        public bool StepRow()
        {
            var rc = Step(handle);
            if (rc == Row)
                return true;

            if (rc != Done)
                throw new InvalidOperationException($"sqlite step failed ({rc}): {conn.Error()}");

            return false;
        }

        public void StepDone()
        {
            this.StepRow();
            Reset(handle);
        }

        public void ResetStatement() => Reset(handle);

        public long GetInt64(int col) => ColumnInt64(handle, col);
        public bool IsNull(int col) => ColumnType(handle, col) == Null;
        public string? GetText(int col) => this.IsNull(col) ? null : Marshal.PtrToStringUTF8(ColumnText(handle, col));

        public void Dispose() => FinalizeStatement(handle);
    }
}
#endif
