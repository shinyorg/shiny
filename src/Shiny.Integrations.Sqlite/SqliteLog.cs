
//using System;
//using Shiny.Infrastructure;
//using Shiny.Logging;
//using Shiny.Models;


//namespace Shiny.Integrations.Sqlite
//{
//    public class SqliteLog : ILogger
//    {
//        readonly ShinySqliteConnection conn;
//        readonly ISerializer serializer;


//        public SqliteLog(ShinySqliteConnection conn, ISerializer serializer)
//        {
//            this.conn = conn;
//            this.serializer = serializer;
//        }


//        public void Write(Exception exception, params (string Key, string Value)[] parameters)
//        {
//            this.conn.GetConnection().Insert(new LogStore
//            {
//                Description = exception.ToString(),
//                Detail = String.Empty,
//                Parameters = this.serializer.Serialize(parameters),
//                IsError = true,
//                TimestampUtc = DateTime.UtcNow
//            });
//        }

//        public void Write(string eventName, string description, params (string Key, string Value)[] parameters)
//        {
//            this.conn.GetConnection().Insert(new LogStore
//            {
//                Description = eventName,
//                Detail = description,
//                Parameters = this.serializer.Serialize(parameters),
//                TimestampUtc = DateTime.UtcNow
//            });
//        }
//    }
//}
