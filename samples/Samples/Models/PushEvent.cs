using SQLite;
using System;


namespace Samples.Models
{
    public class PushEvent
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        public string? Token { get; set; }
        public string? Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
