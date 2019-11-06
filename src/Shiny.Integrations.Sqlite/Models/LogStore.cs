using System;
using SQLite;


namespace Shiny.Models
{
    public class LogStore
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        public string Description { get; set; }
        public string Detail { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Parameters { get; set; }
        public bool IsError { get; set; }
    }
}
