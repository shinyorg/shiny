using System;
using System.IO;
using SQLite;
using Shiny.IO;


namespace $safeprojectname$
{
    public class SqliteConnection : SQLiteAsyncConnection
    {
        public SqliteConnection(IFileSystem fileSystem) : base(Path.Combine(fileSystem.AppData.FullName, "mwl.db"))
        {
        }
    }
}
