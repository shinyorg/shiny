using System;
using Shiny.Jobs;


namespace Shiny.Locations.Sync
{
    static class Extensions
    {
        public static SyncConfig GetSyncConfig(this JobInfo jobInfo)
        {
            var cfg = new SyncConfig 
            {
                BatchSize = jobInfo.GetParameter<int>(nameof(SyncConfig.BatchSize)),
                SortMostRecentFirst = jobInfo.GetParameter<bool>(nameof(SyncConfig.SortMostRecentFirst))
            };
            return cfg;
        }


        public static void SetSyncConfig(this JobInfo jobInfo, SyncConfig config)
        {
            jobInfo.Parameters.Add(nameof(SyncConfig.BatchSize), config.BatchSize);
            jobInfo.Parameters.Add(nameof(SyncConfig.SortMostRecentFirst), config.SortMostRecentFirst);
        }
    }
}
