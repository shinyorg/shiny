using System;


namespace Shiny
{
    public class SyncConfig
    {
        // TODO: only send the latest?
        // sync types - aggressive, normal?
        //public int RetryCount { get; set; } = 0;
        public bool SortMostRecentFirst { get; set; }
        public TimeSpan? ExpirationTime { get; set; }
        public int BatchSize { get; set; } = 1;
    }
}
