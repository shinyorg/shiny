using System;
using SQLite;


namespace Samples.Models
{
    public class BleEvent
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
