using System;


namespace Shiny.Locations.Sync
{
    public class TimedEntity<T>
    {
        public Guid Id { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public T Entity { get; set; }
    }
}
