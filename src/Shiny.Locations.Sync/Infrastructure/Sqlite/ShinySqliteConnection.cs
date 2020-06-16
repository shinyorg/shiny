using System;
using System.IO;
using Shiny.IO;
using SQLite;


namespace Shiny.Locations.Sync.Infrastructure.Sqlite
{
    public class ShinySqliteConnection : SQLiteConnection
    {
        public ShinySqliteConnection(IFileSystem fileSystem) : base(Path.Combine(fileSystem.AppData.FullName, "shinylocsync.db"), true) 
        { 
        }
    }
}
