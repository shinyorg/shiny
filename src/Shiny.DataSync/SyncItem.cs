using System;
using SQLite;


namespace Shiny.DataSync
{
    public class SyncItem
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public string EntityId { get; set; }
        public string TypeName { get; set; }
        public string SerializedEntity { get; set; }
        //public int Attempts { get; set; }
        public SyncOperation Operation { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
