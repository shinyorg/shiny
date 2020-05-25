using System;


namespace Shiny
{
    public class SyncConfig
    {
        // sync types - aggressive, normal?
        //public int RetryCount { get; set; } = 0;
        public bool SortMostRecentFirst { get; set; }
        public TimeSpan? ExpirationTime { get; }
        public int BatchSize { get; set; } = 1;
    }
}
