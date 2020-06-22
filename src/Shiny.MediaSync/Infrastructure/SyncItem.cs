using System;


namespace Shiny.MediaSync.Infrastructure
{
    public class SyncItem
    {
        public string Id { get; set; }
        public string FilePath { get; set; }
        public string HttpTransferId { get; set; }
        public DateTimeOffset DateStarted { get; set; }
    }
}
