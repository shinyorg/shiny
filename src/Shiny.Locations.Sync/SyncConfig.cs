using System;


namespace Shiny
{
    public class SyncConfig
    {
        // sync types - aggressive, normal?
        public bool SendOnlyMostRecent { get; }
        public TimeSpan? ExpirationTime { get; }
        public int RetryCount { get; set; } = 0;
    }
}
