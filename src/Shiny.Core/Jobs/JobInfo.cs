using System;
using System.Collections.Generic;


namespace Shiny.Jobs
{
    public class JobInfo
    {
        public JobInfo(Type jobType, string? identifier = null)
        {
            this.Type = jobType;
            this.Identifier = identifier ?? jobType.AssemblyQualifiedName;
        }


        public TimeSpan? PeriodicTime { get; set; }
        public string Identifier { get; }
        public Type Type { get; }
        public bool Repeat { get; set; } = true;
        public bool DeviceCharging { get; set; }
        public bool BatteryNotLow { get; set; }

        /// <summary>
        /// Calling JobManager.Clear will not remove this task
        /// </summary>
        public bool IsSystemJob { get; set; }
        public InternetAccess RequiredInternetAccess { get; set; } = InternetAccess.None;
        public DateTime? LastRunUtc { get; set; }
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
