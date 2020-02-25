using System;
using System.Collections.Generic;


namespace Shiny.Jobs
{
    public class JobInfo
    {
        public JobInfo(Type jobType, string? identifier = null)
        {
            if (jobType == null)
                throw new ArgumentException("Job Type not set");

            if (String.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Job identifier defined");

            this.Type = jobType;
            this.Identifier = identifier ?? jobType.AssemblyQualifiedName;
            this.PeriodicTime = TimeSpan.FromMinutes(15);
        }


        /// <summary>
        /// Periodic time works with Android & UWP. Though optional, you must provide some form of criteria for the platforms to trigger your job
        /// To prevent breaking changes, this is defaulted to 15 minutes which is the minimum value on UWP & Android
        /// </summary>
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
