using System;


namespace Shiny.Infrastructure
{
    public class RepositoryEvent
    {
        public RepositoryEvent(RepositoryEventType type, string key = null, object entity = null)
        {
            this.Type = type;
            this.Key = key;
            this.Entity = entity;
        }


        public RepositoryEventType Type { get; }
        public string Key { get; }
        public object Entity { get; }
    }
}
