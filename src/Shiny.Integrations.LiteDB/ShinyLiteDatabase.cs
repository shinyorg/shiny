using System;
using System.IO;
using LiteDB;
using Shiny.IO;


namespace Shiny
{
    class ShinyLiteDatabase : LiteDatabase
    {
        public ShinyLiteDatabase(IFileSystem fileSystem, BsonMapper mapper = null, Logger log = null)
            : base(Path.Combine(fileSystem.AppData.FullName, "shiny.db"), mapper, log)
        {
        }
    }
}
