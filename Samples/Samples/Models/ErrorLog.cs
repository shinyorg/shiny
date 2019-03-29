using System;
using SQLite;

namespace Samples.Models
{
    public class ErrorLog
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Parameters { get; set; }
    }
}
