using SQLite;
using System;


namespace Samples.Models
{
    public class LocationSyncEvent
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string Description { get; set; }
        public int Retries { get; set; }
        public DateTime DateLastAttempt { get; set; }
        public DateTime? DateSync { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
