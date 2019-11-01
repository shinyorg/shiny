using System;
using System.Collections.Generic;


namespace Shiny.Jobs
{
    public class JobInfo
    {
        public string Identifier { get; set; }
        public Type Type { get; set; }
        //public bool DeviceIdle { get; set; } // this will only work on droid

        public bool Repeat { get; set; } = true;
        public bool DeviceCharging { get; set; }
        public bool BatteryNotLow { get; set; }

        /// <summary>
        ///
        /// If left null, the suggested time will be used
        ///     iOS -
        ///     Android - Suggested: 15mins - Min: 30 seconds
        ///     UWP - Suggested: 15mins - Min: 15mins
        /// </summary>
        public TimeSpan? PeriodicTime { get; set; }

        /// <summary>
        /// Calling JobManager.Clear will not remove this task
        /// </summary>
        public bool IsSystemJob { get; set; }
        public InternetAccess RequiredInternetAccess { get; set; } = InternetAccess.None;
        public DateTime? LastRunUtc { get; set; }
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
