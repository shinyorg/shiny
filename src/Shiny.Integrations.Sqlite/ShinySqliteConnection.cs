using System;
using System.IO;
using Shiny.IO;
using SQLite;

namespace Shiny
{
    class ShinySqliteConnection : SQLiteAsyncConnection
    {
        public ShinySqliteConnection(IFileSystem fileSystem) : base(Path.Combine(fileSystem.AppData.FullName, "shiny.db")) { }
    }
}
