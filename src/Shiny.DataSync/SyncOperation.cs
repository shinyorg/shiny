using System;


namespace Shiny.DataSync
{
    public class SyncOperation
    {
        public string EntityId { get; }
        public Type EntityType { get; }
        public DateTimeOffset? DateCompleted { get; }
        public DateTimeOffset DateCreated { get; }
    }
}
