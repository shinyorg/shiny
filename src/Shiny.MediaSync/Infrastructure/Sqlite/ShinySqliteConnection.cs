using System;
using System.IO;
using Shiny.IO;
using SQLite;


namespace Shiny.MediaSync.Infrastructure.Sqlite
{
    public class ShinySqliteConnection : SQLiteConnection
    {
        public ShinySqliteConnection(IFileSystem fileSystem) : base(Path.Combine(fileSystem.AppData.FullName, "shinymediasync.db"), true) 
        { 
        }
    }
}
